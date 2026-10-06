using System.Globalization;
using Xunit;

namespace PropStruct.Cli.Tests;

/// <summary>
/// L0 of the BOOT.md table: "numbers parse the same under a decimal-comma culture" (BOOT.md,
/// Constraints: "Parsing is culture-invariant, as Input's is"). The repository builds with
/// <c>InvariantGlobalization=true</c> (Directory.Build.props), under which a *named* culture cannot be
/// looked up, so the decimal-comma culture here is built by cloning the invariant culture and overwriting
/// only its separator - the same technique <c>tests/Output.Tests</c> uses for the same reason.
/// </summary>
public class CultureInvarianceTests
{
    private static CultureInfo DecimalComma()
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NumberDecimalSeparator = ",";
        return culture;
    }

    private static readonly string[] DotDecimalArgs = ["run", "input.dat", "--eps", "0.05", "--seed", "12"];

    [Fact]
    public void DotDecimalNumbersStillParseUnderADecimalCommaCurrentCulture()
    {
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = DecimalComma();
        try
        {
            var result = CommandLine.Parse(DotDecimalArgs);
            Assert.True(result.Success, string.Join("; ", result.Errors.Select(e => e.Message)));
            var command = Assert.IsType<RunCommand>(result.Command);
            Assert.Equal(0.05, command.Parameters.EpsDok);
            Assert.Equal(12UL, command.Options.Seed);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    private static readonly string[] CommaDecimalArgs = ["run", "input.dat", "--eps", "0,05"];

    [Fact]
    public void CommaDecimalNumbersAreNeverAcceptedEvenUnderThatCurrentCulture()
    {
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = DecimalComma();
        try
        {
            var result = CommandLine.Parse(CommaDecimalArgs);
            Assert.False(result.Success);
            Assert.Contains(result.Errors, e => e.Diagnostic == Diagnostic.InvalidNumber);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
