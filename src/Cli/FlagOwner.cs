namespace PropStruct.Cli;

/// <summary>Which record a flag ultimately sets a property of, or whether it is a control of the tool
/// itself with no model or run-option counterpart (<c>--output</c>, <c>--json</c>, <c>--quiet</c>). Only
/// <see cref="ModelParameters"/> and <see cref="SimulationOptions"/> entries take part in the reflected
/// "every property has a flag, every flag has a property" check (BOOT.md, Acceptance criteria).</summary>
internal enum FlagOwner
{
    ModelParameters,
    SimulationOptions,
    Cli,
}
