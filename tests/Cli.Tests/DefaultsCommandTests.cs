using PropStruct.Input;
using Xunit;

namespace PropStruct.Cli.Tests;

/// <summary>
/// L0/L1 of the BOOT.md table: "`propstruct defaults` prints exactly `ModelParameters.Default`... canonical
/// name and aliases in two columns". <see cref="ReflectedFlagCoverageTests"/> only ever calls
/// <see cref="DefaultsReport.Build"/>; this file is the rest of the claim - <see cref="DefaultsReport.Render"/>'s
/// actual text, an alias column, and the `defaults` verb itself through <see cref="Program.Run"/>.
/// </summary>
public class DefaultsCommandTests
{
    [Fact]
    public void RenderListsAnAliasNextToItsCanonicalFlag()
    {
        var text = DefaultsReport.Render(ModelParameters.Default);

        // --di (menu [2]) has the alias --cell-size (FlagCatalog.ModelParameterFlags): the row that
        // starts the canonical flag must also carry the alias text on the same line.
        var line = Assert.Single(text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries), l => l.Contains("--di", StringComparison.Ordinal));
        Assert.Contains("--cell-size", line, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderListsDashForAFlagWithNoAlias()
    {
        var text = DefaultsReport.Render(ModelParameters.Default);

        // --dmin (menu [1]) has no alias.
        var line = Assert.Single(text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries), l => l.Contains("--dmin", StringComparison.Ordinal));
        Assert.Contains("-", line, StringComparison.Ordinal);
    }

    private static readonly string[] DefaultsArgs = ["defaults"];

    [Fact]
    public void DefaultsVerbPrintsExactlyWhatRenderProduces()
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var exitCode = Program.Run(DefaultsArgs, stdout, stderr, CancellationToken.None);

        Assert.Equal(ExitCode.Success, exitCode);
        Assert.Empty(stderr.ToString());
        Assert.Equal(DefaultsReport.Render(ModelParameters.Default), stdout.ToString());
    }
}
