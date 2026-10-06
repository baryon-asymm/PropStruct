using Xunit;

namespace PropStruct.Tests.Harness.Tests;

/// <summary>
/// <see cref="CudaRequirement"/>: the variable decides, only the exact value <c>1</c> requires CUDA, and the reason
/// the fact gives reaches the failure. The cases inject the environment, so no test of this assembly changes the
/// process's own variable, which the release's GPU job sets for the whole run.
/// </summary>
public class CudaRequirementTests
{
    private static Func<string, string?> Environment(string? value) =>
        name => name == CudaRequirement.VariableName ? value : null;

    [Fact]
    public void TheVariableIsTheOneTheReleaseJobSets() => Assert.Equal("PROPSTRUCT_REQUIRE_CUDA", CudaRequirement.VariableName);

    [Fact]
    public void FailIfRequiredThrowsNamingTheReasonWhenTheVariableIsOne()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => CudaRequirement.FailIfRequiredIn("no libdevice", Environment("1")));

        Assert.Contains("no libdevice", exception.Message, StringComparison.Ordinal);
        Assert.Contains(CudaRequirement.VariableName, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("0")]
    [InlineData("")]
    [InlineData("true")]
    [InlineData("11")]
    public void FailIfRequiredReturnsWhenTheVariableIsUnsetOrNotOne(string? value)
    {
        Assert.False(CudaRequirement.IsRequiredIn(Environment(value)));
        CudaRequirement.FailIfRequiredIn("no device", Environment(value));
    }

    [Fact]
    public void IsRequiredIsTrueForOneOnly() => Assert.True(CudaRequirement.IsRequiredIn(Environment("1")));

    [Fact]
    public void TheProcessEnvironmentPathAgreesWithTheVariable()
    {
        var variable = System.Environment.GetEnvironmentVariable(CudaRequirement.VariableName);

        Assert.Equal(variable == "1", CudaRequirement.IsRequired);
        if (variable == "1")
        {
            _ = Assert.Throws<InvalidOperationException>(() => CudaRequirement.FailIfRequired("reason"));
        }
        else
        {
            CudaRequirement.FailIfRequired("reason");
        }
    }
}
