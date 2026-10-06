using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Execution.Tests;

/// <summary>
/// Every fact of this node that returns when no CUDA accelerator is available calls
/// <see cref="CudaRequirement.FailIfRequired(string)"/> (BOOT.md, "Acceptance criteria"): the branches are found
/// in the node's own sources by <see cref="CudaRefusalBranches"/>, not listed here.
/// </summary>
public sealed class CudaRequirementWiringTests
{
    [Fact]
    public void EveryBranchOnARefusedCudaEngineCallsFailIfRequired()
    {
        var scan = CudaRefusalBranches.Scan(RepositoryPaths.Resolve("tests", "Execution.Tests"));

        Assert.True(scan.Branches > 0, "no branch on CudaSkippedBecause was found: the scan reads nothing.");
        Assert.True(scan.Unguarded.Count == 0, "these branches return on a refused CUDA engine without CudaRequirement.FailIfRequired: " + string.Join(", ", scan.Unguarded));
    }
}
