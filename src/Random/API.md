# API.md — Random

Namespace `PropStruct.Random`. The GSV=2 generator of the original, jump-ahead and a
particle's six streams. Surface internal to the tree (`InternalsVisibleTo` for
`Particle`, `Execution`, `Simulation`, their tests and `ILGPURuntime`).

## Generator ✅

```csharp
internal struct Mcg128State { public ulong Low; public ulong High; }

internal static class Mcg128
{
    public static Mcg128State A40 { get; }                              // a^(2⁴⁰) mod 2¹²⁸, tabulated for OriginalSeeds.ForParticle
    public static Mcg128State A80 { get; }                              // a^(2⁸⁰) mod 2¹²⁸, tabulated for OriginalSeeds.ForParticle
    public static Mcg128State A38 { get; }                              // a^(2³⁸) mod 2¹²⁸, tabulated for OriginalSeeds.ForBatchedParticle
    public static double Next(ref Mcg128State state);                   // step, then the original's ten-term sum
    public static Mcg128State Advance(Mcg128State state, ulong kLow, ulong kHigh);
    public static Mcg128State Multiply(Mcg128State x, Mcg128State y);   // mod 2¹²⁸
}

internal struct StreamSet { public Mcg128State S1, S2, S3, S4, S5, S6; }

internal static class OriginalSeeds
{
    public static StreamSet Streams { get; }                          // lines 52–63 as written
    public static StreamSet ForParticle(ulong seed, ulong ordinal);   // seed·2⁸⁰ + ordinal·2⁴⁰ steps ahead
    public static StreamSet ForBatchedParticle(ulong seed, ulong ordinal); // + role-group offset g(r)·2³⁸, BOOT.md "Batched derivation"
}
```

`Next` returns a value in [0, 1]. It is exactly 1.0 for a fraction 2⁻⁵⁴ of states, for
example 2¹²⁸ − 2²⁸ + 1, which lies on stream 6's orbit: the ten-term sum rounds up in
`double`, as the original's REAL*8 sum does. No call can fail.

⚠ 2026-09-24: was "[0, 1)". Found by the fidelity audit of 2026-09-24 and checked by
replaying the sum on that state.

## Independent layout ✅

```csharp
internal enum StreamLayout { Original, Independent }

internal static class IndependentSeeds
{
    public static StreamSet Streams { get; }                          // H_r·2²⁸ + (2r+1), BOOT.md "Independent layout"
    public static StreamSet ForParticle(ulong seed, ulong ordinal);   // seed·2⁸⁰ + ordinal·2⁴⁰ steps ahead
}

internal static class StreamSeeds
{
    public static StreamSet ForParticle(StreamLayout layout, ulong seed, ulong ordinal);
    public static StreamSet ForBatchedParticle(StreamLayout layout, ulong seed, ulong ordinal); // BOOT.md "Batched derivation"
}
```

## Side effects

None.

## Out of scope

- Which stream feeds which draw: `Particle`.
- GSV=1 and GSV=3 of the original.
