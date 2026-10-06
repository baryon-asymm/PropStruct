using System.Globalization;

namespace PropStruct.Benchmarks;

/// <summary>The outcome of measuring one (formulation, path) pair.</summary>
internal abstract record FigureOutcome;

/// <summary>A figure obtained by actually running something, with its spread across
/// <paramref name="Repeats"/> repeats (API.md: "runs per figure, whose spread is recorded with
/// it"). <paramref name="WarmupIncluded"/> is true when the recorded window could not be
/// isolated to cycle 1 alone (<see cref="CycleBoundaryRecorder"/>'s fallback) or, for
/// <see cref="BenchmarkPath.Original"/>, is always true by the nature of that row (this node's
/// BOOT.md, "Original's particles/s") and is not called out per figure there.
///
/// <paramref name="AttemptsPerLaunch"/>, <paramref name="Launches"/> and <paramref name="Attempts"/>
/// are Decision B's own addition (2026-09-27): the budget passed to <c>SimulationOptions</c> and
/// the launch/attempt totals <c>SimulationResult.Diagnostics</c> reports for it, both null only
/// for <see cref="BenchmarkPath.Original"/>, which never builds a <c>SimulationOptions</c> at all.
/// Every repeat shares one seed (0, the default), so under "Deterministic under every schedule"
/// (root BOOT.md) these three are identical across repeats; the last repeat's values are kept,
/// not averaged — there is nothing to average.</summary>
internal sealed record Measured(
    double MeanParticlesPerSecond, double SpreadParticlesPerSecond, int Repeats, bool WarmupIncluded,
    int? AttemptsPerLaunch, long? Launches, long? Attempts) : FigureOutcome;

/// <summary>A path this machine cannot exercise right now (no CUDA device, no legacy
/// executable) — a per-machine, per-run condition, reported as "not measured on this machine"
/// rather than a blank or a zero (API.md).</summary>
internal sealed record NotMeasuredOnThisMachine(string Reason) : FigureOutcome;

/// <summary>A path this node cannot reach at all yet, regardless of machine: a gap in a
/// dependency's public surface, not an environment condition (BOOT.md, "## Escalation: the CPU
/// accelerator oracle path"). Deliberately worded differently from
/// <see cref="NotMeasuredOnThisMachine"/> so the two causes are not read as the same thing.</summary>
internal sealed record NotAvailable(string Reason) : FigureOutcome;

internal sealed record FigureRow(string Formulation, BenchmarkPath Path, FigureOutcome Outcome)
{
    /// <summary>Renders one line in the shape of BOOT.md's own table
    /// (<c>| Date | Formulation | Path | Accepted particles/s | Budget | Launches | Attempts |
    /// Machine | Build | Commit |</c>), ready to paste into it (API.md).</summary>
    public string ToMarkdownRow(Provenance provenance)
    {
        var figure = Outcome switch
        {
            Measured m => FormatMeasured(m),
            NotMeasuredOnThisMachine n => $"not measured on this machine ({n.Reason})",
            NotAvailable n => $"not available ({n.Reason})",
            _ => throw new InvalidOperationException($"unhandled outcome {Outcome}"),
        };

        var (budget, launches, attempts) = Outcome switch
        {
            Measured m => (
                m.AttemptsPerLaunch?.ToString(CultureInfo.InvariantCulture) ?? "n/a",
                m.Launches?.ToString(CultureInfo.InvariantCulture) ?? "n/a",
                m.Attempts?.ToString(CultureInfo.InvariantCulture) ?? "n/a"),
            _ => ("n/a", "n/a", "n/a"),
        };

        return $"| {provenance.DateUtc} | {Formulation} | {Path.ToToken()} | {figure} | {budget} | " +
               $"{launches} | {attempts} | {provenance.Machine} | Release | {provenance.Commit} |";
    }

    private string FormatMeasured(Measured measured)
    {
        var figure = string.Create(CultureInfo.InvariantCulture,
            $"{measured.MeanParticlesPerSecond:F0} ± {measured.SpreadParticlesPerSecond:F0} (n={measured.Repeats})");

        // The original's own row always covers the whole run, warm-up included: that is
        // documented once, in BOOT.md, not repeated on every one of its rows. A port path that
        // fell back to including the warm-up is the exceptional case and is called out here.
        if (measured.WarmupIncluded && Path != BenchmarkPath.Original)
        {
            figure += ", warm-up not isolated";
        }

        return figure;
    }
}
