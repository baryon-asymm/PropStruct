namespace PropStruct.Simulation;

/// <summary>
/// Reported after every batch, and after every 1000 particles in reference mode (BOOT.md, "Design
/// decisions (2026-09-18)", "Progress and cancellation"). <see cref="Cycle"/> is the cycle currently
/// running (0 is the warm-up); <see cref="Cycles"/> is the run's total post-warm-up cycle count (the
/// formulation's own <c>KXX</c>); <see cref="AcceptedParticles"/> is the count accepted so far over the
/// whole run.
/// </summary>
public readonly record struct CycleProgress(int Cycle, int Cycles, long AcceptedParticles);
