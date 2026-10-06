using PropStruct.Input;
using Xunit;

namespace PropStruct.Cli.Tests;

/// <summary>
/// L0 of the BOOT.md table: "a length reaches <see cref="ModelParameters"/> as written (<c>--dmin
/// 10e-6</c> != <c>--dmin 10</c>, the first equal to the default)". The expected default value comes from
/// <c>tests/Fixtures/cases/input/model-parameters-default.json</c> (<c>dminMetres: 1e-5</c>) via
/// <see cref="ModelParameters.Default"/> itself, never typed twice.
/// </summary>
public class LengthAsWrittenTests
{
    private static RunCommand Parse(string dminText)
    {
        var result = CommandLine.Parse(new[] { "run", "input.dat", "--dmin", dminText });
        Assert.True(result.Success, string.Join("; ", result.Errors.Select(e => e.Message)));
        return Assert.IsType<RunCommand>(result.Command);
    }

    [Fact]
    public void TenTimesTenToTheMinusSixEqualsTheDefaultWrittenAsMetres()
    {
        var command = Parse("10e-6");
        Assert.Equal(ModelParameters.Default.Dmin.AsWritten, command.Parameters.Dmin.AsWritten);
        Assert.False(command.Parameters.Dmin.IsMicrometres);
    }

    [Fact]
    public void TenIsADifferentRunWrittenAsMicrometres()
    {
        var command = Parse("10");
        Assert.Equal(10.0, command.Parameters.Dmin.AsWritten);
        Assert.True(command.Parameters.Dmin.IsMicrometres);
        Assert.NotEqual(ModelParameters.Default.Dmin.AsWritten, command.Parameters.Dmin.AsWritten);
    }

    [Fact]
    public void TenEMinusSixAndZeroPointZeroZeroZeroZeroOneAreTheSameRun()
    {
        // 10e-6 and 0.00001 both parse to the same double: the flag carries the parsed double, not the
        // text (BOOT.md's own acceptance criterion, parenthetical).
        var fromExponent = Parse("10e-6");
        var fromDecimal = Parse("0.00001");
        Assert.Equal(fromDecimal.Parameters.Dmin.AsWritten, fromExponent.Parameters.Dmin.AsWritten);
    }
}
