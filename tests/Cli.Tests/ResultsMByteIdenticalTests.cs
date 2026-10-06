using PropStruct.Execution;
using PropStruct.Input;
using PropStruct.Output;
using PropStruct.Simulation;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Cli.Tests;

/// <summary>
/// L1 of the BOOT.md table: "`propstruct run` on a reference formulation writes a <c>results.m</c>
/// byte-identical to the one the library writes for the same options (<c>Output</c>), time line
/// excluded". A whole reference-mode run of a reference formulation, so <c>Category=Long</c> (measured:
/// several seconds), the same reason <c>tests/Output.Tests</c>' own reference-formulation row carries it.
/// </summary>
public class ResultsMByteIdenticalTests
{
    private const string Formulation = "inpt"; // the smallest reference formulation (root BOOT.md, "Statistical reference criterion").

    [Fact]
    [Trait("Category", "Long")]
    public void CliRunWritesTheSameResultsMAsALibraryRunTimeLineExcluded()
    {
        var datPath = RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", Formulation + ".dat");
        var formulation = DatFile.Read(datPath);

        var libraryOptions = new SimulationOptions
        {
            Mode = ExecutionMode.Reference,
            Accelerator = AcceleratorKind.Cpu,
            Streams = StreamLayout.Original,
            Seed = 0UL,
        };

        using var simulator = Simulator.Create(libraryOptions);
        var libraryResult = simulator.Run(formulation);

        using var directory = new TemporaryDirectory();
        var libraryPath = Path.Combine(directory.Path, "library.m");
        ResultsMWriter.Write(formulation, ModelParameters.Default, libraryResult, libraryPath);

        var cliPath = Path.Combine(directory.Path, "cli.m");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var exitCode = Program.Run(
            new[]
            {
                "run", datPath, "--mode", "reference", "--accelerator", "cpu",
                "--layout", "original", "--seed", "0", "--quiet", "--output", cliPath,
            },
            stdout, stderr, CancellationToken.None);

        Assert.Equal(ExitCode.Success, exitCode);

        // Byte for byte, not line by line (reviewed 2026-09-20: File.ReadAllLines re-splits on either
        // line-ending convention and would not notice a CRLF/LF difference between the two writers).
        var libraryBytes = ResultsMTimeLine.Remove(File.ReadAllBytes(libraryPath));
        var cliBytes = ResultsMTimeLine.Remove(File.ReadAllBytes(cliPath));
        Assert.Equal(libraryBytes, cliBytes);
    }
}
