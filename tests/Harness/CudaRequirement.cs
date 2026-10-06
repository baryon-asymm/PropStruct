namespace PropStruct.Tests.Harness;

/// <summary>
/// The switch that turns a CUDA fact's early return into a failure. A fact that runs on CUDA where an
/// accelerator is available and asserts the refusal where it is not returns green on a machine without
/// CUDA; in the release's GPU job that is the one outcome that must not pass, because the job exists to
/// run on CUDA. The job sets <see cref="VariableName"/> to <c>1</c>, and each such fact calls
/// <see cref="FailIfRequired(string)"/> on the branch where CUDA was refused.
/// </summary>
public static class CudaRequirement
{
    /// <summary>The environment variable the release's GPU job sets.</summary>
    public const string VariableName = "PROPSTRUCT_REQUIRE_CUDA";

    /// <summary>True when <see cref="VariableName"/> is set to exactly <c>1</c>.</summary>
    public static bool IsRequired => IsRequiredIn(Environment.GetEnvironmentVariable);

    /// <summary>
    /// For a fact that returns when no CUDA accelerator is available: throws when <see cref="IsRequired"/>.
    /// </summary>
    /// <param name="reason">Why CUDA is not available, as the fact knows it; it is named in the message.</param>
    /// <exception cref="InvalidOperationException"><see cref="IsRequired"/> and the fact reached the refused branch.</exception>
    public static void FailIfRequired(string reason) => FailIfRequiredIn(reason, Environment.GetEnvironmentVariable);

    internal static bool IsRequiredIn(Func<string, string?> environment) =>
        environment(VariableName) == "1";

    internal static void FailIfRequiredIn(string reason, Func<string, string?> environment)
    {
        if (IsRequiredIn(environment))
        {
            throw new InvalidOperationException($"{VariableName}=1: CUDA is required here and was refused: {reason}");
        }
    }
}
