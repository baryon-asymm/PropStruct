namespace PropStruct.Statistics;

/// <summary>
/// Every quantity <c>results.m</c> prints that is not an input, cycle bookkeeping or
/// print-time arithmetic (BOOT.md, "## Report"), in SI units, with normalized
/// distributions as fractions per cell. In cycle 0 the fields of "Cycles >= 1" below
/// are <see cref="double.NaN"/>, matching the original: <c>CycleStatistics.Compute</c>
/// never assigns them for <c>cycleIndex == 0</c>. The report of the last cycle is the
/// run's result.
/// </summary>
internal sealed record CycleReport
{
    // Setup echoes.
    public required double Dokm { get; init; }
    public required double Doksd { get; init; }
    public required double Ddokmax { get; init; }
    public required double Dmax { get; init; }
    public required double TailProbabilityModified { get; init; }
    public required double Ggg { get; init; }
    public required double[] Zx { get; init; }

    // Counters as they stand.
    public required long Qkss { get; init; } // QKSS, the original's INTEGER*8.
    public required long Nfx { get; init; }
    public required long Nfy { get; init; }
    public required long Nfz { get; init; }
    public required long Nfq { get; init; }
    public required long Nfw { get; init; }
    public required long[] Conditions { get; init; }
    public required long IbridgeTotal { get; init; }
    public required double JammedTotal { get; init; }
    public required double NnTotal { get; init; }

    // Accuracies.
    public required double Eps1 { get; init; }
    public required double Eps2 { get; init; }
    public required double Eps3 { get; init; }
    public required double Eps4 { get; init; }
    public required double Eps5 { get; init; }
    public required double Eps6 { get; init; }
    public required double Eps7 { get; init; }
    public required double Epsx1 { get; init; }
    public required double Epsx2 { get; init; }
    public required double Epsx3 { get; init; }
    public required double[] Epsalldok { get; init; }
    public required double Epsy { get; init; }
    public required double Epsmd4 { get; init; }
    public required double Epsmd3 { get; init; }
    public required bool OxidizerAccuracyWarning { get; init; }
    public required bool PocketAccuracyWarning { get; init; }

    // Sizes.
    public required double Dok43b { get; init; }
    public required double Dok43s { get; init; }
    public required double Alldok43 { get; init; }
    public required double Alldok432 { get; init; }
    public required double Dp43 { get; init; }
    public required double D432 { get; init; }
    public required double Sdevp43 { get; init; }
    public required double Dqkarm { get; init; }
    public required double Dkarm43Cor { get; init; } // Cycles >= 1.
    public required double Sdevp43Cor { get; init; } // Cycles >= 1.
    public required double DqkarmCor { get; init; } // Cycles >= 1.
    public required double Dfmk432 { get; init; } // Cycles >= 1.
    public required double Sdevp243 { get; init; } // Cycles >= 1.
    public required double Dqmkm1 { get; init; } // Cycles >= 1.
    public required double Dqmkm2 { get; init; } // Cycles >= 1.
    public required double Qmcoef { get; init; } // Cycles >= 1.

    // Matrix.
    public required double Gdokleft { get; init; }
    public required double Vdokleft { get; init; }
    public required double Plotsmdok { get; init; }
    public required double Plotsm { get; init; }
    public required double Mp { get; init; }

    // Mass fractions (cycles >= 1).
    public required double DolM1 { get; init; }
    public required double DolM2 { get; init; }
    public required double DolM3 { get; init; }

    // Distributions, as fractions per cell.
    public required double[] Allvdokso { get; init; } // Ndok.
    public required double[] Vkso { get; init; } // Nkarm.
    public required double[] Qks1 { get; init; } // Nkarm: this cycle's own Qks, freshly normalized (not the live input QKS1 Particle used mid-cycle).
    public required double[] FmkarmCorNormalized { get; init; } // Nkarm, cycles >= 1.
    public required double[] Fmkarm2Normalized { get; init; } // Nkarm, cycles >= 1.
    public required double[] FqkarmCorNormalized { get; init; } // Nkarm.
    public required double[] Qmkm1Normalized { get; init; } // Nc, cycles >= 1.
    public required double[] Qmkm2Normalized { get; init; } // Nc, cycles >= 1.
    public required double[] CoefNormalized { get; init; } // Nc, cycles >= 1.
    public required double[] Pdoksmall { get; init; } // Ndok - 1 (Fortran line 1386, BOOT.md "## Report").
    public required double DpMax { get; init; }
    public required double DpMaxCor { get; init; }

    /// <summary>The 1-based largest non-empty index of <see cref="CoefNormalized"/>'s underlying integer total (the original's <c>coef_nmax</c>), 0 when empty.</summary>
    public required int CoefNmax { get; init; }

    /// <summary>The 1-based largest non-empty index of <see cref="Qmkm1Normalized"/>'s underlying integer total (<c>qmkm1_nmax</c>), 0 when empty.</summary>
    public required int Qmkm1Nmax { get; init; }

    /// <summary>The 1-based largest non-empty index of <see cref="Qmkm2Normalized"/>'s underlying integer total (<c>qmkm2_nmax</c>), 0 when empty.</summary>
    public required int Qmkm2Nmax { get; init; }

    // Categories (BOOT.md, "## Categories"): trimmed to the final DpRow rows.
    public required int DpRow { get; init; }
    public required double[] Dpockets { get; init; } // DpRow.
    public required double[] Dokp43 { get; init; } // DpRow.
    public required double[] Qdokkarm { get; init; } // DpRow.
    public required double[][] Qdokso { get; init; } // DpRow rows, each Ndok long (CA1814: jagged, not multidimensional).

    // Convergence (cycles >= 1, Fortran lines 1168-1174).
    public required double ConvergenceEpsy { get; init; }
    public required double ConvergenceEpsmd3 { get; init; }
    public required double ConvergenceEpsmd4 { get; init; }
    public required double ConvergenceAlldok43 { get; init; }
    public required double ConvergenceAlldoksd { get; init; }
    public required double ConvergenceDolM2 { get; init; }
}
