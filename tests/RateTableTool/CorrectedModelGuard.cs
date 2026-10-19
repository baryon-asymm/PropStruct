using PropStruct.Simulation;
using PropStruct.Tests.Harness;

namespace PropStruct.RateTableTool;

/// <summary>
/// The cases of the change detector of the <c>Corrected</c> model and the approved file they are compared against
/// (<c>tests/Simulation.Tests/Snapshots/CorrectedModelSnapshot.approved.txt</c>): written by this node's
/// <c>corrected-model-snapshot</c> subcommand, read by <c>tests/Simulation.Tests/CorrectedModelGuardTests</c>
/// (BOOT.md, "## The Corrected-model snapshot"). The mechanism is the Original-model guard's, <see cref="ApprovedGuardFile"/>
/// and <see cref="ReferenceModeRunner.Fingerprint"/>; the cases are those of <see cref="OriginalModelGuard"/> that the
/// model kind can move, run with <see cref="SimulationOptions.Model"/> set to <see cref="ModelKind.Corrected"/> and no
/// seam, which is <c>CorrectionSet.All</c> (<c>ModelKindTests</c>). It is a change detector, not a reproduction claim: the
/// <c>Corrected</c> model has no external reference.
/// </summary>
internal static class CorrectedModelGuard
{
    /// <summary>The prefix of every id of the file, so that a line of it is never taken for a line of the Original guard's.</summary>
    private const string IdPrefix = "corrected/";

    /// <summary>
    /// The cases of <see cref="OriginalModelGuard.Cases"/> run under <c>Corrected</c>: the reduced extras the references
    /// leave dark (every <c>extra/...</c> case at <c>Binary64</c> but <c>P312-homogenized0.1</c>, which the other P312
    /// cases already cover) and the two batched references, the snapshot's only batched cases (they showed the withdrawn
    /// <c>CategoriesEmptyRowsMerged</c> and still see five other flags; <c>tests/Simulation.Tests/ACCEPTANCE.md</c>, the
    /// per-flag table). <c>Corrected</c> is refused under the <c>Original</c> precision kind, so no <c>Original</c>-precision
    /// case can be here.
    /// </summary>
    private static readonly string[] SourceIds =
    [
        "extra/P312-defaults",
        "extra/P312-dmin5",
        "extra/P312-cycles3",
        "extra/PSAN02-sfr-dmin20",
        "extra/PSAN02-sfr-dmin20-cycles3",
        "extra/PSAN02-sfr-dmin20-di10",
        "extra/P33-defaults",
        "extra/P2-defaults",
        "batched/inpt/Independent/Binary64",
        "batched/PSAN02n/Independent/Binary64",
    ];

    public static IReadOnlyList<GuardCase> Cases { get; } =
    [.. SourceIds.Select(id => OriginalModelGuard.Find(id) with { Id = IdPrefix + id })];

    public static string ApprovedPath => RepositoryPaths.Resolve(
        "tests", "Simulation.Tests", "Snapshots", "CorrectedModelSnapshot.approved.txt");

    public static GuardCase Find(string id) => Cases.Single(c => string.Equals(c.Id, id, StringComparison.Ordinal));

    /// <summary>The options of a run of <paramref name="guardCase"/> under the <c>Corrected</c> model with no seam.</summary>
    public static SimulationOptions Options(GuardCase guardCase) => guardCase.Options() with { Model = ModelKind.Corrected };

    /// <summary>The line the approved file carries for <paramref name="guardCase"/>.</summary>
    public static string Line(GuardCase guardCase, ReferenceModeRunner.Run run) => ApprovedGuardFile.Line(guardCase.Id, run);

    /// <summary>The approved hashes by case id; empty when the file does not exist.</summary>
    public static Dictionary<string, (string ResultsM, string Json)> LoadApproved() => ApprovedGuardFile.Load(ApprovedPath);
}
