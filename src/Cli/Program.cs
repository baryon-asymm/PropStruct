using System.Globalization;
using System.Reflection;
using PropStruct.Execution;
using PropStruct.Input;
using PropStruct.Output;
using PropStruct.Simulation;

namespace PropStruct.Cli;

/// <summary>
/// The shell of the tool (BOOT.md, Constraints: "`Program` is a shell: it parses, runs and maps the
/// outcome to an exit code"). <see cref="Main"/> is the one line the design calls for; every actual
/// behaviour is in <see cref="Run"/>, reachable in-process so <c>tests/Cli.Tests</c> can drive the whole
/// tool without starting a process (the assembly's own <c>InternalsVisibleTo</c>).
/// </summary>
internal static class Program
{
    private static int Main(string[] args) => Run(args, Console.Out, Console.Error, CancellationToken.None);

    /// <summary>
    /// Parses <paramref name="args"/>, runs the requested verb and returns the exit code (BOOT.md,
    /// "Mapping of outcomes onto exit codes"). Ctrl+C is taken here, not in <see cref="Main"/>, so it
    /// composes with whatever cancellation the caller already asked for (a test cancels
    /// <paramref name="cancellationToken"/> directly; a real process additionally has
    /// <see cref="Console.CancelKeyPress"/> wired to the same linked source).
    /// </summary>
    public static int Run(string[] args, TextWriter stdout, TextWriter stderr, CancellationToken cancellationToken)
    {
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        void onCancelKeyPress(object? _, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            linkedSource.Cancel();
        }

        Console.CancelKeyPress += onCancelKeyPress;
        try
        {
            return Dispatch(args, stdout, stderr, linkedSource.Token);
        }
        finally
        {
            Console.CancelKeyPress -= onCancelKeyPress;
        }
    }

    /// <summary>
    /// Parses and dispatches to a verb, with a single try/catch around the whole thing, not just `run`.
    ///
    /// ⚠ 2026-09-20 (review): this used to wrap only <c>ExecuteRun</c>'s own work. <c>devices</c> calls
    /// into <c>Execution</c> (<see cref="AcceleratorProbe.Discover"/>), which is as capable of throwing as
    /// anything <c>run</c> does, and every verb can be cancelled before its own work starts; neither case
    /// was reachable through exit 4 or 130 before. Exit codes are now a property of the whole dispatch,
    /// not of one verb.
    /// </summary>
    private static int Dispatch(string[] args, TextWriter stdout, TextWriter stderr, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var parseResult = CommandLine.Parse(args);
            if (!parseResult.Success)
            {
                foreach (var error in parseResult.Errors)
                {
                    stderr.WriteLine(error.Message);
                }

                if (parseResult.Errors.Any(e => e.Diagnostic is Diagnostic.NoVerbGiven or Diagnostic.UnknownVerb))
                {
                    stdout.WriteLine(UsageText.General);
                }

                return ExitCode.InvalidArguments;
            }

            switch (parseResult.Command)
            {
                case HelpCommand help:
                    stdout.WriteLine(UsageText.ForVerb(help.Verb));
                    return ExitCode.Success;

                case VersionCommand:
                    stdout.WriteLine(InformationalVersion());
                    return ExitCode.Success;

                case DevicesCommand:
                    cancellationToken.ThrowIfCancellationRequested();
                    stdout.Write(DevicesReport.Render(AcceleratorProbe.Discover()));
                    return ExitCode.Success;

                case DefaultsCommand:
                    stdout.Write(DefaultsReport.Render(ModelParameters.Default));
                    return ExitCode.Success;

                case RunCommand run:
                    return ExecuteRun(run, stderr, cancellationToken);

                default:
                    throw new InvalidOperationException($"Unhandled command type {parseResult.Command?.GetType()}.");
            }
        }
        catch (Exception ex) when (!IsProcessFatal(ex))
        {
            return MapExceptionToExitCode(ex, inputPath: null, stderr);
        }
    }

    /// <summary>
    /// The two catch-alls below narrowed by a <c>when</c> filter (CA1031, owner's decision
    /// 2026-09-24; BOOT.md, "Errors"): everything the library does not name as one of its
    /// documented failures still becomes exit 4 ("an unhandled exception ... a defect of the
    /// tool"), except the two process-fatal exceptions here, which propagate instead — mapping
    /// them to a value would claim the process is still in a state where returning a value means
    /// anything. A separate method, not an inline expression, so a test can exercise the
    /// predicate itself (`tests/Cli.Tests/ProcessFatalExceptionTests`) without driving a real
    /// <see cref="OutOfMemoryException"/> or <see cref="InsufficientExecutionStackException"/>
    /// through the dispatcher, which would tear down the test host.
    /// </summary>
    internal static bool IsProcessFatal(Exception exception) =>
        exception is OutOfMemoryException or InsufficientExecutionStackException;

    private static int ExecuteRun(RunCommand command, TextWriter stderr, CancellationToken cancellationToken)
    {
        if (TryFindMissingOutputDirectory(command.OutputPath, out var missingOutputDirectory))
        {
            stderr.WriteLine($"output directory does not exist: {missingOutputDirectory}");
            return ExitCode.InfrastructureError;
        }

        if (command.JsonPath is not null && TryFindMissingOutputDirectory(command.JsonPath, out var missingJsonDirectory))
        {
            stderr.WriteLine($"output directory does not exist: {missingJsonDirectory}");
            return ExitCode.InfrastructureError;
        }

        Formulation formulation;
        try
        {
            formulation = DatFile.Read(command.InputPath, command.Parameters.ReadPocketFormingFractions);
        }
        catch (Exception ex) when (ex is FormulationFormatException or FileNotFoundException or IOException or UnauthorizedAccessException)
        {
            // "missing, unreadable or malformed input file" is exit 2 (BOOT.md's own Errors table), even
            // though IOException/UnauthorizedAccessException mean exit 3 a few lines further down, once
            // they come from *writing* an output instead. The phase decides the code, not the exception
            // type alone - reviewed 2026-09-20: one shared catch used to cover both phases and reading a
            // permission-denied input landed on exit 3.
            return MapReadFailureToExitCode(ex, command.InputPath, stderr);
        }

        try
        {
            using var simulator = Simulator.Create(command.Options);

            var progress = command.Quiet
                ? null
                : new SynchronousProgress<CycleProgress>(report =>
                    stderr.WriteLine(FormattableString.Invariant($"cycle {report.Cycle} of {report.Cycles}, particles {report.AcceptedParticles}")));

            var result = simulator.Run(formulation, progress, cancellationToken);

            AtomicFile.Write(command.OutputPath, path => ResultsMWriter.Write(formulation, command.Parameters, result, path));
            if (command.JsonPath is not null)
            {
                AtomicFile.Write(command.JsonPath, path => ResultsJson.Write(result, path));
            }

            if (!command.Quiet)
            {
                stderr.WriteLine(BuildSummaryLine(command, result));
            }

            return ExitCode.Success;
        }
        catch (Exception ex) when (!IsProcessFatal(ex))
        {
            return MapExceptionToExitCode(ex, command.InputPath, stderr);
        }
    }

    /// <summary>Reading the input file failed: always exit 2, whatever the exception (BOOT.md's Errors
    /// table: "missing, unreadable or malformed input file ... exit 2").</summary>
    private static int MapReadFailureToExitCode(Exception exception, string inputPath, TextWriter stderr)
    {
        if (exception is FormulationFormatException formatException)
        {
            stderr.WriteLine($"{inputPath}:{formatException.LineNumber}: {formatException.Message}");
        }
        else
        {
            stderr.WriteLine(exception.Message);
        }

        return ExitCode.InvalidArguments;
    }

    /// <summary>
    /// BOOT.md, "Mapping of outcomes onto exit codes", for every route except reading the input file
    /// (<see cref="MapReadFailureToExitCode"/>'s own, always exit 2) and the two output-directory checks
    /// (always exit 3, decided before either try block runs). Takes <paramref name="inputPath"/> rather
    /// than a whole <c>RunCommand</c> so it also covers exceptions from verbs that have no
    /// <c>RunCommand</c> at all (<c>devices</c>); testable directly with a constructed exception - the
    /// only practical way to drive the catch-all "a defect of the tool" branch (exit 4), which by
    /// construction has no legitimate cause to reproduce end to end.
    /// </summary>
    internal static int MapExceptionToExitCode(Exception exception, string? inputPath, TextWriter stderr)
    {
        switch (exception)
        {
            case OperationCanceledException:
                stderr.WriteLine("interrupted");
                return ExitCode.Interrupted;

            case FormulationFormatException ex:
                stderr.WriteLine($"{inputPath}:{ex.LineNumber}: {ex.Message}");
                return ExitCode.InvalidArguments;

            case FileNotFoundException ex:
                stderr.WriteLine(ex.Message);
                return ExitCode.InvalidArguments;

            case SimulationFailedException ex:
                return ReportFailedRun(ex, stderr);

            case ArgumentException ex:
                // src/Simulation/API.md's own Errors table: "invalid options (non-positive budgets, batch
                // size) -> ArgumentException before any work". BOOT.md's ⚠ 2026-09-20 narrows "the tool
                // never checks a parameter's range or a combination of options" to name the two checks
                // this node does make itself (budget positivity, --continued-streams' combination); this
                // branch is for whatever the library still rejects that way regardless. Reviewed
                // 2026-09-20: previously unhandled here, landing on exit 4 ("a defect of the tool") for
                // what is really invalid user input.
                stderr.WriteLine(ex.Message);
                return ExitCode.InvalidArguments;

            case UnauthorizedAccessException ex:
                stderr.WriteLine(ex.Message);
                return ExitCode.InfrastructureError;

            case IOException ex:
                stderr.WriteLine(ex.Message);
                return ExitCode.InfrastructureError;

            default:
                stderr.WriteLine($"{exception.GetType()}: {exception.Message}");
                return ExitCode.UnhandledException;
        }
    }

    /// <summary>Internal (not private) so the reflected non-degeneracy check in <c>tests/Cli.Tests</c> can
    /// confirm every <see cref="RunStatus"/> that reaches <see cref="ReportFailedRun"/>'s <c>default</c>
    /// case has an entry, without a hand-typed mirror of the enum.</summary>
    internal static readonly IReadOnlyDictionary<RunStatus, string?> BudgetFlagOf = new Dictionary<RunStatus, string?>
    {
        [RunStatus.AttemptCapExceeded] = "--max-attempts-per-particle",
        [RunStatus.NeighbourBudgetExceeded] = "--neighbour-budget",
        [RunStatus.BridgeDrawBudgetExceeded] = null,
        [RunStatus.IndexOutOfRange] = null,
        [RunStatus.CategoryCountExceedsCapacity] = null,
    };

    private static int ReportFailedRun(SimulationFailedException ex, TextWriter stderr)
    {
        switch (ex.Status)
        {
            case RunStatus.AcceleratorUnavailable:
                stderr.WriteLine($"accelerator unavailable: {ex.Message}");
                return ExitCode.InfrastructureError;

            case RunStatus.InvalidSetup:
                stderr.WriteLine($"invalid setup: {ex.Message}");
                return ExitCode.InvalidArguments;

            case RunStatus.Ok:
            case RunStatus.AttemptCapExceeded:
            case RunStatus.NeighbourBudgetExceeded:
            case RunStatus.BridgeDrawBudgetExceeded:
            case RunStatus.IndexOutOfRange:
            case RunStatus.CategoryCountExceedsCapacity:
            default:
                var flag = BudgetFlagOf.GetValueOrDefault(ex.Status);
                stderr.WriteLine(flag is null
                    ? $"run failed: {ex.Status}"
                    : $"run failed: {ex.Status} (raise it with {flag})");
                return ExitCode.RunFailed;
        }
    }

    private static bool TryFindMissingOutputDirectory(string path, out string? missingDirectory)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            missingDirectory = directory;
            return true;
        }

        missingDirectory = null;
        return false;
    }

    private static string BuildSummaryLine(RunCommand command, SimulationResult result)
    {
        var diagnostics = result.Diagnostics;
        var files = command.JsonPath is null ? command.OutputPath : $"{command.OutputPath}, {command.JsonPath}";
        var batch = diagnostics.BatchSize.ToString(CultureInfo.InvariantCulture);
        var seed = diagnostics.Seed.ToString(CultureInfo.InvariantCulture);
        var attempts = diagnostics.TotalAttempts.ToString(CultureInfo.InvariantCulture);
        var elapsed = diagnostics.Elapsed.ToString("c", CultureInfo.InvariantCulture);

        return $"mode={diagnostics.Mode} accelerator={diagnostics.Accelerator.Kind} batch={batch} " +
               $"layout={diagnostics.Streams} seed={seed} attempts={attempts} elapsed={elapsed} files={files}";
    }

    private static string InformationalVersion() =>
        typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(Program).Assembly.GetName().Version?.ToString(3)
        ?? "0.0.0";
}
