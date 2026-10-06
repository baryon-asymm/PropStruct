using System.Globalization;

namespace PropStruct.Benchmarks;

/// <summary>The parsed command line (API.md, "## Command line"). <see cref="Particles"/> is
/// not in API.md's original sketch; it is this coding session's own addition (BOOT.md,
/// "## Cycle 1 isolation"), needed to verify the plumbing on a few hundred particles instead of
/// a formulation's full, and often much larger, shipped <c>N</c> — and useful beyond
/// verification, to trade a smaller, noisier figure for a shorter run. It overrides the port
/// paths' particle count only (see <see cref="LegacyRunner"/>). <see cref="AttemptsPerLaunch"/>
/// is this session's own addition too (Decision B, 2026-09-27: "tests/Benchmarks has no budget
/// flag; the propstruct CLI does"), overriding <c>SimulationOptions.AttemptsPerLaunch</c> for the
/// port paths only; the original executable and the ILGPU CPU accelerator's own kernel-launch
/// path have no such knob reachable from this node.</summary>
internal sealed record CommandLineOptions(
    IReadOnlyList<string> Formulations, IReadOnlyList<BenchmarkPath> Paths, int Repeats, int? Particles,
    int? AttemptsPerLaunch)
{
    private static readonly IReadOnlyList<string> DefaultFormulations = ["HPEPA3", "HMX"];

    private static readonly IReadOnlyList<BenchmarkPath> DefaultPaths =
    [
        BenchmarkPath.Reference, BenchmarkPath.Host1, BenchmarkPath.Host16,
        BenchmarkPath.Cpu, BenchmarkPath.Cuda, BenchmarkPath.Original,
    ];

    public static CommandLineOptions? Parse(string[] args, TextWriter error)
    {
        List<string> formulations = [.. DefaultFormulations];
        List<BenchmarkPath> paths = [.. DefaultPaths];
        var repeats = 3;
        int? particles = null;
        int? attemptsPerLaunch = null;

        for (var i = 0; i < args.Length; i++)
        {
            var flag = args[i];
            string? Value()
            {
                if (i + 1 >= args.Length)
                {
                    error.WriteLine($"propstruct benchmarks: '{flag}' needs a value");
                    return null;
                }
                return args[++i];
            }

            try
            {
                switch (flag)
                {
                    case "--formulation":
                        if (Value() is not { } formulationList)
                        {
                            return null;
                        }

                        formulations = [.. Split(formulationList)];
                        break;

                    case "--paths":
                        if (Value() is not { } pathList)
                        {
                            return null;
                        }

                        paths = [.. Split(pathList).Select(BenchmarkPathNames.Parse)];
                        break;

                    case "--repeats":
                        if (Value() is not { } repeatsText)
                        {
                            return null;
                        }

                        repeats = int.Parse(repeatsText, CultureInfo.InvariantCulture);
                        break;

                    case "--particles":
                        if (Value() is not { } particlesText)
                        {
                            return null;
                        }

                        particles = int.Parse(particlesText, CultureInfo.InvariantCulture);
                        break;

                    case "--attempts-per-launch":
                        if (Value() is not { } attemptsPerLaunchText)
                        {
                            return null;
                        }

                        attemptsPerLaunch = int.Parse(attemptsPerLaunchText, CultureInfo.InvariantCulture);
                        break;

                    default:
                        error.WriteLine($"propstruct benchmarks: unrecognized argument '{flag}'");
                        return null;
                }
            }
            catch (FormatException)
            {
                error.WriteLine($"propstruct benchmarks: '{flag}' expects an integer");
                return null;
            }
            catch (ArgumentException ex)
            {
                error.WriteLine($"propstruct benchmarks: {ex.Message}");
                return null;
            }
        }

        if (repeats < 1)
        {
            error.WriteLine("propstruct benchmarks: --repeats must be at least 1");
            return null;
        }

        if (particles is <= 0)
        {
            error.WriteLine("propstruct benchmarks: --particles must be positive");
            return null;
        }

        if (attemptsPerLaunch is <= 0)
        {
            error.WriteLine("propstruct benchmarks: --attempts-per-launch must be positive");
            return null;
        }

        if (formulations.Count == 0 || paths.Count == 0)
        {
            error.WriteLine("propstruct benchmarks: --formulation and --paths need at least one entry");
            return null;
        }

        return new CommandLineOptions(formulations, paths, repeats, particles, attemptsPerLaunch);
    }

    private static string[] Split(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
