using Xunit;

namespace PropStruct.Tests.Harness.Tests;

/// <summary>
/// <see cref="CudaRefusalBranches"/>: red on a branch that returns without the call, right on one that makes it,
/// each shape of the condition once, and the real sources of the nodes that carry CUDA facts.
/// </summary>
public class CudaRefusalBranchesTests
{
    private static (int Line, bool Guarded) Only(string source)
    {
        var branch = Assert.Single(CudaRefusalBranches.Branches(source));
        return (branch.Line, branch.Guarded);
    }

    [Fact]
    public void ARefusedBlockWithoutTheCallIsUnguardedAtItsLine()
    {
        const string source = "void F()\n{\n    if (engine.Accelerator.CudaSkippedBecause is not null)\n    {\n        Assert.True(true);\n        return;\n    }\n}\n";

        Assert.Equal((3, false), Only(source));
    }

    [Fact]
    public void ARefusedBlockThatCallsFailIfRequiredIsGuarded()
    {
        const string source = "if (x.CudaSkippedBecause is not null)\n{\n    CudaRequirement.FailIfRequired(x.CudaSkippedBecause);\n    return;\n}\n";

        Assert.Equal((1, true), Only(source));
    }

    [Fact]
    public void TheCallAfterTheBlockDoesNotGuardIt()
    {
        const string source = "if (x.CudaSkippedBecause is not null)\n{\n    return;\n}\nCudaRequirement.FailIfRequired(\"late\");\n";

        Assert.False(Only(source).Guarded);
    }

    [Fact]
    public void ANestedBraceInsideTheBlockDoesNotEndItEarly()
    {
        const string source = "if (x.CudaSkippedBecause is not null)\n{\n    foreach (var a in b) { Use(a); }\n    CudaRequirement.FailIfRequired(\"r\");\n}\n";

        Assert.True(Only(source).Guarded);
    }

    [Fact]
    public void ASingleStatementBranchIsRead()
    {
        Assert.True(Only("if (x.CudaSkippedBecause is not null) CudaRequirement.FailIfRequired(\"r\");\n").Guarded);
        Assert.False(Only("if (x.CudaSkippedBecause is not null) return;\n").Guarded);
    }

    [Fact]
    public void ATrailingCommentAfterTheConditionIsAllowed() =>
        Assert.True(Only("if (e.CudaSkippedBecause is not null) // the engine's own outcome\n{\n    CudaRequirement.FailIfRequired(\"r\");\n}\n").Guarded);

    [Fact]
    public void ForIsNullTheRefusedSideIsTheElse()
    {
        Assert.True(Only("if (x.CudaSkippedBecause is null)\n{\n    Assert.True(x.Bound);\n}\nelse\n{\n    CudaRequirement.FailIfRequired(\"r\");\n}\n").Guarded);
        Assert.False(Only("if (x.CudaSkippedBecause is null)\n{\n    CudaRequirement.FailIfRequired(\"in the bound side\");\n}\nelse\n{\n    Assert.True(true);\n}\n").Guarded);
    }

    [Fact]
    public void ForIsNullWithoutAnElseTheRefusedSideIsEmptyAndUnguarded() =>
        Assert.False(Only("if (x.CudaSkippedBecause is null)\n{\n    Assert.True(x.Bound);\n}\n").Guarded);

    [Fact]
    public void CommentedConditionsAndAssertionsAreNotBranches()
    {
        const string source = "/// branches on <c>engine.Accelerator.CudaSkippedBecause</c>\n// if (x.CudaSkippedBecause is not null) { }\nAssert.NotNull(x.CudaSkippedBecause);\nvar y = new Info(CudaSkippedBecause: null);\n";

        Assert.Empty(CudaRefusalBranches.Branches(source));
    }
}
