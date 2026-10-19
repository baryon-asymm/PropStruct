namespace PropStruct.Particle;

/// <summary>
/// The explicit errors of the original that the <c>Corrected</c> model fixes, one flag each
/// (BOOT.md, "## Corrected model"; API.md, "## Corrected model"). The values are explicit
/// powers of two and are never renumbered or reused; <see cref="All"/> is their sum. The
/// value 4 is retired: 2026-10-18, it was <c>Fmkarm2OnAcceptance</c> (BOOT.md,
/// "## Corrected model"); so is the value 1024: 2026-10-19, it was
/// <c>CategoriesEmptyRowsMerged</c> (<c>Statistics</c>' BOOT.md, "## Corrected model"). A member is named in its defect rows' <c>Corrected</c> column as
/// <c>fixed (&lt;Member&gt;)</c>. <c>Attempt.Run</c> acts on <see cref="ExactConstants"/>,
/// <see cref="BridgeFrustum"/> and <see cref="WindowLowerEdge"/> only; the others fix lines of
/// <c>Statistics</c> and mean nothing here.
/// </summary>
[Flags]
internal enum CorrectionSet : uint
{
    /// <summary>The <c>Original</c> model; an unset <see cref="ModelSetup.Corrections"/> is this.</summary>
    None = 0,

    /// <summary>A literal approximating π or 1/3 is that constant (this node, <c>Statistics</c>).</summary>
    ExactConstants = 1,

    /// <summary>Fortran 1616–1627: the bridge volume as the frustum less the two caps.</summary>
    BridgeFrustum = 2,

    /// <summary>Fortran 595: the window's origin at the cell's lower edge.</summary>
    WindowLowerEdge = 8,

    /// <summary><c>Statistics</c>, Fortran 1145, 1150.</summary>
    VdokTotalOnCopy = 16,

    /// <summary><c>Statistics</c>, Fortran 1047.</summary>
    ZdoksmallCoarseShare = 32,

    /// <summary><c>Statistics</c>, Fortran 1001–1006.</summary>
    GdokleftOnce = 64,

    /// <summary><c>Statistics</c>, Fortran 399–400, 405.</summary>
    DoksdConsistent = 128,

    /// <summary><c>Statistics</c>, Fortran 911–959.</summary>
    CategoriesOnCopy = 256,

    /// <summary><c>Statistics</c>, Fortran 947–959.</summary>
    CategoriesLastRowRecomputed = 512,

    /// <summary><c>Statistics</c>, Fortran 1059–1061.</summary>
    PdoksmallFirstCell = 2048,

    /// <summary>The <c>Corrected</c> model: every member above.</summary>
    All = 3067,
}
