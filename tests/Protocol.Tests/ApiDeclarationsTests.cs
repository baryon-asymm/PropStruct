using Xunit;

namespace PropStruct.Protocol.Tests;

/// <summary>
/// The declaration parser on the shapes an <c>API.md</c> block holds that no real document is guaranteed to
/// keep: a method whose return type is a tuple must yield the method's own name, however the tuple and the
/// parameter list are broken across lines, and never a member named after a modifier. The inputs are the
/// shapes of <c>tests/Harness/API.md</c>, the first place that held them.
/// </summary>
public sealed class ApiDeclarationsTests
{
    [Fact]
    public void ATupleReturningMethodOnOneLineYieldsItsName()
    {
        const string Block = "internal static class Band\n{\n    internal static (long Low, long High) Region(long n, double p);\n}";

        Assert.Equal(["Band", "Region"], Names(Block));
    }

    [Fact]
    public void ATupleReturningMethodWithAParameterListOverSeveralLinesYieldsItsNameOnce()
    {
        const string Block = "internal static class Counts\n{\n"
            + "    internal static (List<Dictionary<int, double>> PerReplica, List<double> Totals) PerCell(\n"
            + "        IReadOnlyList<string> cells, string arrayName,\n"
            + "        IReadOnlyList<double?> totals);\n"
            + "    internal static int Next(int k);\n}";

        Assert.Equal(["Counts", "PerCell", "Next"], Names(Block));
    }

    [Fact]
    public void ATupleOverSeveralLinesAndANestedTupleYieldTheNameAfterTheBalancedParenthesis()
    {
        const string Block = "internal static class Pairs\n{\n"
            + "    internal static (\n        double Low,\n        (int X, int Y) Corner) Spread(double width);\n"
            + "    internal static ((int X, int Y) Corner, double Size) Nested(double width);\n}";

        Assert.Equal(["Pairs", "Spread", "Nested"], Names(Block));
    }

    [Fact]
    public void ATupleTypedFieldIsNotAMethodAndNoModifierIsEverAName()
    {
        const string Block = "internal static class Holder\n{\n    internal static readonly (int A, int B) Origin = (0, 0);\n"
            + "    public static int Plain(int k);\n}";

        var names = Names(Block);

        Assert.Equal(["Holder", "Plain"], names);
        Assert.DoesNotContain("static", names);
        Assert.DoesNotContain("readonly", names);
    }

    [Fact]
    public void ANewModifierBeforeATupleReturningMethodYieldsItsName()
    {
        const string Block = "public class Derived\n{\n    public new (int A, int B) Band(int count);\n"
            + "    public static new (int A, int B) Edge(int count);\n}";

        Assert.Equal(["Derived", "Band", "Edge"], Names(Block));
    }

    [Fact]
    public void ANewModifierAmongOtherModifiersYieldsTheMemberNameAndNeverTheWordNew()
    {
        const string Block = "public class Derived\n{\n    public new int Plain(int x);\n"
            + "    public new static (int A, int B) Two();\n    public new static int Three(int x);\n}";

        var names = Names(Block);

        Assert.Equal(["Derived", "Plain", "Two", "Three"], names);
        Assert.DoesNotContain("new", names);
    }

    [Fact]
    public void ANewExpressionIsNeverAMemberName()
    {
        const string Block = "public class Holder\n{\n    public static readonly (int A, int B) Origin = new (0, 0);\n"
            + "    public int Plain(int k);\n}";

        var names = Names(Block);

        Assert.Equal(["Holder", "Plain"], names);
        Assert.DoesNotContain("new", names);
    }

    private static List<string> Names(string block) => ApiDeclarations.Declarations(block).Select(declaration => declaration.Name).ToList();
}
