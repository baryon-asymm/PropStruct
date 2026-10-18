namespace PropStruct.Statistics.Tests;

/// <summary>
/// Lines 1050-1061 of the original for one cell, in closed form, as the tests of the small-particle probability
/// restate them: the single place of this node's tests that holds the formula.
/// </summary>
internal static class SmallParticlesClosedForm
{
    /// <summary>
    /// <c>vdoksmall ddoksmall karmcoef</c> for a cell whose scaled <c>zdoksmall</c> is <paramref name="z"/> and whose
    /// <c>ddoksmall</c> is <paramref name="d"/>: <c>vdoksmall = z/(S2 + z) PL/PLOT1</c> with <c>S1 = GGG - gdokleft</c>,
    /// <c>S2 = 1 - GGG + gdokleft</c> and <c>PL = (1 - (S1 - z))/(1/PLOT2 - (S1 - z)/PLOT1)</c>.
    /// </summary>
    public static double Product(
        double z, double d, double ggg, double gdokleft, double karmcoef, double oxidizerDensity, double propellantDensity)
    {
        var s1 = ggg - gdokleft;
        var s2 = 1.0 - ggg + gdokleft;
        var pl = (1.0 - (s1 - z)) / (1.0 / propellantDensity - (s1 - z) / oxidizerDensity);
        return z / (s2 + z) * pl / oxidizerDensity * d * karmcoef;
    }
}
