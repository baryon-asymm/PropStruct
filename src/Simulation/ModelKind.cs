namespace PropStruct.Simulation;

/// <summary>
/// Which model a run computes (root BOOT.md, "Model kind is an option of every run"; BOOT.md,
/// "## Corrected model"). "Kind", parallel to <see cref="PrecisionKind"/>: "variant" already names the
/// menu parameter <c>ivar</c> (<c>ModelParameters.Variant</c>).
/// </summary>
public enum ModelKind
{
    /// <summary>The original's model, bit for bit; the default.</summary>
    Original = 0,

    /// <summary>The model with the original's explicit errors fixed, each a row marked fixed in the Corrected column of docs/ORIGINAL-DEFECTS.md.</summary>
    Corrected = 1,
}
