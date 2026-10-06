using PropStruct.Particle;

namespace PropStruct.Statistics;

/// <summary>
/// The probability of a small oxidizer particle sitting inside a pocket around a base
/// particle, and the largest base size that probability still reaches (Fortran lines
/// 1021-1074). Host code, double precision (BOOT.md, Constraints).
/// </summary>
internal static class SmallParticles
{
    /// <summary>
    /// Fortran lines 1021-1065: <c>zdoksmall</c>, <c>ddoksmall</c>, <c>vdoksmall</c> and
    /// <c>pdoksmall</c>, monotone from index 1 (0-based; index 0 is the original's
    /// <c>pdoksmall(1)</c>, never assigned by the original and always 0 in the port,
    /// BOOT.md "Decisions where the port departs from a transcription"). <c>ak2</c> is
    /// the formulation's own <c>AK2</c> (precondition <c>AK2 &gt; 1</c> keeps every
    /// index below within <paramref name="ndok"/>, BOOT.md, Constraints). Under
    /// <see cref="PrecisionKind.Original"/> <paramref name="precision"/> rounds the sites
    /// <c>CyclePlane.listing.generated.txt</c> names in these lines through
    /// <see cref="CyclePlaneRounding"/>; under <see cref="PrecisionKind.Binary64"/> nothing
    /// changes.
    /// </summary>
    public static double[] Probability(
        int ndok, double cellSize, double ak2, double ggg, double gdokleft, double karmcoef,
        double oxidizerDensity, double propellantDensity, double[] vdokstr, PrecisionKind precision)
    {
        var rounding = new CyclePlaneRounding(precision);
        var vdokstrSum = 0.0;
        foreach (var v in vdokstr)
        {
            vdokstrSum += v;
        }

        var zdoksmall = new double[ndok];
        var ddoksmall = new double[ndok];
        for (var k = 0; k < ndok; k++)
        {
            var kilo = k + 1; // Fortran 1-based loop variable.
            var realKilo = rounding.Temporary(kilo); // Fortran 1025
            var xr = rounding.Store(Mod(realKilo, ak2) / ak2); // Fortran 1025
            // Fortran `int(real(kilo)/ak2)`: kilo converted to REAL and AK2 read as
            // REAL*4 (root BOOT.md, "Double precision only", the binary32 exception
            // extended to this whole-fraction count), the same integer-mantissa
            // truncation Setup.Sizes already uses for Ndok/Nkarm/Ncat and Categories
            // now uses for DPRow.
            var wholeFractions = (int)Binary32.TruncatedQuotient(kilo, ak2);

            // 1028: the sum is carried in the register and copied to the home every pass, so
            // the home holds the rounded final sum and 1031 re-reads it.
            var sum = 0.0;
            for (var iks = 1; iks <= wholeFractions; iks++)
            {
                sum += vdokstr[iks - 1] / vdokstrSum;
            }

            zdoksmall[k] = rounding.Store(sum); // Fortran 1028

            // 1034 compares the register of 1031's sum, and 1036 and 1039 divide by it;
            // the home, read by the later blocks, holds it rounded.
            var zdoksmallRegister = zdoksmall[k] + vdokstr[wholeFractions] / vdokstrSum * xr;
            zdoksmall[k] = rounding.Store(zdoksmallRegister); // Fortran 1031

            if (zdoksmallRegister > rounding.Literal(1e-5)) // Fortran 1034
            {
                var weighted = 0.0;
                for (var iks = 1; iks <= wholeFractions; iks++)
                {
                    weighted += vdokstr[iks - 1] / vdokstrSum / zdoksmallRegister * cellSize * (iks - 0.5);
                }

                ddoksmall[k] = rounding.Store(weighted); // Fortran 1036
                ddoksmall[k] = rounding.Store(ddoksmall[k] + vdokstr[wholeFractions] * xr / vdokstrSum / zdoksmallRegister * cellSize * (wholeFractions + 0.5 * xr)); // Fortran 1039
            }
        }

        for (var k = 0; k < ndok; k++)
        {
            zdoksmall[k] = rounding.Store(zdoksmall[k] * ggg); // Fortran 1047
        }

        // The compiler's temporaries, recomputed from the same operands: T1 and T2 are hoisted
        // before the cycle loop, S1 and S2 are spilled once per cycle.
        var t1 = rounding.Temporary(1.0 / propellantDensity); // Fortran 1008
        var t2 = rounding.Temporary(1.0 - ggg); // Fortran 1010
        var s1 = rounding.Temporary(ggg - gdokleft); // Fortran 1006
        var s2 = rounding.Temporary(t2 + gdokleft); // Fortran 1010
        var vdoksmall = new double[ndok];
        for (var k = 0; k < ndok; k++)
        {
            var kilo = k + 1;
            var pl = (1.0 - (s1 - zdoksmall[k])) / (t1 - (s1 - zdoksmall[k]) / oxidizerDensity);
            vdoksmall[k] = rounding.Store(zdoksmall[k] / (s2 + zdoksmall[k]) * pl / oxidizerDensity); // Fortran 1053
            ddoksmall[k] = rounding.Store(ddoksmall[k] / (cellSize * kilo)); // Fortran 1056
        }

        var pdoksmall = new double[ndok];
        pdoksmall[0] = 0.0; // pdoksmall(1): never assigned by the original (port decision, BOOT.md).
        for (var k = 1; k < ndok; k++)
        {
            // 1062 compares this product, still in the register, with the previous element's home,
            // which holds a rounded value (the table's compare of site 40F485). Against the rounded
            // product it would choose the same branch: binary32 rounding is monotone.
            var pdoksmallInBlock = vdoksmall[k] * ddoksmall[k] * karmcoef;
            pdoksmall[k] = rounding.Store(pdoksmallInBlock); // Fortran 1061
            if (pdoksmallInBlock < pdoksmall[k - 1])
            {
                pdoksmall[k] = pdoksmall[k - 1];
            }
        }

        return pdoksmall;
    }

    /// <summary>Fortran lines 1066-1074: the largest base size the small-particle probability still reaches. <c>zdmaxxx</c> (line 1071) is never read by anything and is not ported (BOOT.md, "Line map").</summary>
    public static double MaxSize(int ndok, double cellSize, double ddokmax, double karmcoef, double mkmcoef, double[] pdoksmall, PrecisionKind precision)
    {
        var rounding = new CyclePlaneRounding(precision);
        var dmaxxx = ddokmax;
        for (var k = 0; k < ndok; k++)
        {
            if (mkmcoef / karmcoef * pdoksmall[k] >= 1.0)
            {
                dmaxxx = rounding.Store(cellSize * (k + 1)); // Fortran 1070
                break;
            }
        }

        return dmaxxx;
    }

    private static double Mod(double a, double b) => a - Math.Floor(a / b) * b;
}
