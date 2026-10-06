// snippet-start: QuickstartUsings
using PropStruct.Execution;
using PropStruct.Input;
using PropStruct.Output;
using PropStruct.Simulation;
// snippet-end

namespace PropStruct.Quickstart;

/// <summary>
/// The library example as a program (BOOT.md, Purpose): the one region between the snippet markers of
/// <see cref="Example"/> is quoted verbatim by the root <c>API.md</c> and by the package README, and runs
/// against the project references or against the packed <c>PropStruct</c> package alike.
/// </summary>
internal static class Program
{
    /// <summary>Runs the example in the working directory, which holds <c>inpt.dat</c>, and prints the paths
    /// of the two files it wrote. Returns 0 for a run whose status is ok, 1 for a failed status, 2 when the
    /// formulation cannot be read.</summary>
    public static int Main()
    {
        try
        {
            Example();
        }
        catch (SimulationFailedException failed)
        {
            Console.Error.WriteLine($"the run failed with status {failed.Status}");
            return 1;
        }
        catch (Exception unreadable) when (unreadable is IOException or FormulationFormatException)
        {
            Console.Error.WriteLine(unreadable.Message);
            return 2;
        }

        Console.WriteLine(Path.GetFullPath("results.m"));
        Console.WriteLine(Path.GetFullPath("results.json"));
        return 0;
    }

    private static void Example()
    {
        // snippet-start: Quickstart
        var formulation = DatFile.Read("inpt.dat");

        var parameters = ModelParameters.Default with { EpsDok = 0.02 };

        var options = new SimulationOptions
        {
            Parameters = parameters,
            Mode = ExecutionMode.Batched,          // or ExecutionMode.Reference
            Accelerator = AcceleratorKind.Auto,    // Auto, Cpu, Cuda
            BatchSize = null,                      // null: the whole cycle, capped by the record budget
            Streams = StreamLayout.Independent,    // the default; Original runs in reference mode only
            Precision = PrecisionKind.Binary64,    // the default; Original runs in reference mode only
            Seed = 0,                              // 0: the layout's initial states
        };

        using var simulator = Simulator.Create(options);
        var result = simulator.Run(formulation);   // SimulationFailedException on a failed status

        ResultsMWriter.Write(formulation, parameters, result, "results.m");
        ResultsJson.Write(result, "results.json");
        // snippet-end
    }
}
