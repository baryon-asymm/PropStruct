namespace PropStruct.Simulation.Tests;

/// <summary>
/// One printed quantity of <c>results.m</c> the measurement reports a shift for: a cell of
/// <see cref="PropStruct.Tests.Harness.ResultsMFile.Parse(string)"/>'s dictionary, or the length of one of its
/// arrays (<c>DPRow</c> is the length of <c>Dkarmcat</c>, which the writer prints as one row per category).
/// </summary>
internal sealed record HeadlineQuantity(string Label, string Key, int Index, bool IsLength)
{
    /// <summary>
    /// The quantities the task names, in the order of the tables. <c>pdoksmall</c> enters twice: its first printed
    /// cell, which the original leaves unassigned (<see cref="PropStruct.Particle.CorrectionSet.PdoksmallFirstCell"/>),
    /// and, per arm, the cell that moves most (<see cref="StrongestPdoksmallCell"/>).
    /// </summary>
    public static IReadOnlyList<HeadlineQuantity> All { get; } = new[]
    {
        Cell("Dkarm43", "Dkarm43(1)"),
        Cell("Dkarm10", "Dkarm10"),
        Cell("Zkarm", "Zkarm"),
        Cell("Zkarm_cor", "Zkarm_cor"),
        Cell("Dagg43", "Dagg43(1)"),
        Cell("da_coef", "da_coef"),
        Cell("Dqmkm2", "Dqmkm2"),
        Cell("Dok43sd", "Dok43sd"),
        Cell("fineoxy_fr", "fineoxy_fr"),
        new HeadlineQuantity("pdoksmall[0]", "pdoksmall", 0, false),
        new HeadlineQuantity("DPRow", "Dkarmcat", 0, true),
    };

    /// <summary>The first element of the printed quantity <paramref name="key"/>.</summary>
    public static HeadlineQuantity Cell(string label, string key) => new(label, key, 0, false);

    /// <summary>The cell <paramref name="index"/> of <c>pdoksmall</c>, for the one that moves most under an arm.</summary>
    public static HeadlineQuantity StrongestPdoksmallCell(int index) =>
        new($"pdoksmall[{index}]", "pdoksmall", index, false);

    /// <summary>The value in <paramref name="cells"/>, or <see langword="null"/> when the run does not print it.</summary>
    public double? Read(IReadOnlyDictionary<string, double[]> cells)
    {
        if (!cells.TryGetValue(Key, out var values))
        {
            return null;
        }

        if (IsLength)
        {
            return values.Length;
        }

        return Index < values.Length ? values[Index] : null;
    }
}
