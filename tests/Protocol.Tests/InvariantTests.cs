using System.Reflection;
using Xunit;

namespace PropStruct.Protocol.Tests;

/// <summary>
/// Root invariants that need reflection to check (root <c>BOOT.md</c>, Invariants and Taboos; AGENTS.md §13
/// asks for these by name alongside the four generic checks): double precision only (with the one named
/// exception below), no hidden state via a mutable static field in the numerical nodes, and no CUDA type
/// outside <c>Execution</c>. They read the same shapes and bodies <see cref="DependencyTests"/> does.
/// </summary>
public sealed class InvariantTests
{
    /// <summary>The numerical nodes (root <c>BOOT.md</c>, Decomposition: what runs inside a particle thread,
    /// kernel-compatible and low, plus what runs once per batch or cycle on the host above it) the "double
    /// precision only" and "no hidden state" invariants apply to. <c>Input</c>, <c>Output</c> and <c>Cli</c> are
    /// file-format and command-line nodes, not numerical ones.</summary>
    public static readonly IReadOnlyList<string> NumericalNodes = ["src/Random", "src/Particle", "src/Statistics", "src/Simulation", "src/Execution"];

    /// <summary>The one node allowed to name CUDA types (<c>src/Execution/BOOT.md</c>, Invariants: "Only this
    /// node names ILGPU.Runtime.Cuda; no ILGPU type is on its public surface").</summary>
    public const string CudaNode = "src/Execution";

    /// <summary>The ILGPU namespaces that exist only for NVIDIA hardware, as opposed to the accelerator-neutral
    /// ILGPU types (<c>ArrayView</c>, <c>Index1D</c>, the CPU accelerator, …) every kernel-compatible node may
    /// use freely.</summary>
    public static readonly IReadOnlyList<string> CudaNamespaces = ["ILGPU.Runtime.Cuda", "ILGPU.Backends.PTX"];

    /// <summary>
    /// The single-precision exception the root's Taboos carve out, and the only one it carves out (root
    /// <c>BOOT.md</c>, Taboos: "the one exception is the binary32 rounding of the <c>Original</c> precision
    /// kind, a parameter of the one particle program, setup and per-cycle computation, confined to the sites
    /// its three generated classifications name" —
    /// <c>Attempt.AddReal4</c>, <c>src/Particle</c>). Matched
    /// by node, declaring type and method name, and by nothing looser: not "any method whose name contains
    /// Real4", not "any method of <c>Attempt</c>", not "<c>src/Particle</c> at large". <see cref="ConvR4Count"/>
    /// is the exact number of <c>conv.r4</c> instructions the exempted method's body may hold, not a floor, so a
    /// second narrowing cast slipped into the same method still turns
    /// <see cref="NumericalNodesHoldNoSinglePrecisionValueOrOperation"/> red; any other single-precision
    /// opcode or bound type in that method (a stray <c>float</c> literal, field or parameter) does too, exemption
    /// or not.
    ///
    /// This list holds exactly one entry, and
    /// <see cref="NumericalNodesHoldNoSinglePrecisionValueOrOperation"/> asserts that with
    /// <c>Assert.Single</c> before it asserts anything else, and separately asserts that the entry named here
    /// still matches a real method body (a removed or renamed site fails by name, not by an empty, vacuously
    /// green problem list). <strong>To add a second exemption:</strong> name its node, type, method and
    /// <c>conv.r4</c> count here, and widen the <c>Assert.Single</c> call below to the new count deliberately, in
    /// the same diff — do not loosen the match in <see cref="SinglePrecisionProblems"/> itself (by name, by node,
    /// or by opcode count) to let a second site through without both of those edits being visible to a reviewer.
    /// </summary>
    internal static readonly IReadOnlyList<SinglePrecisionExemption> PermittedSinglePrecisionSites =
    [
        new("src/Particle", "PropStruct.Particle.Attempt", "AddReal4", ConvR4Count: 1),
    ];

    [Fact]
    public void NumericalNodesHoldNoSinglePrecisionValueOrOperation()
    {
        _ = Assert.Single(PermittedSinglePrecisionSites); // the exemption is named and singular (root BOOT.md, Taboos); see PermittedSinglePrecisionSites for how to widen it deliberately.

        var seenExemptions = new HashSet<SinglePrecisionExemption>();
        var problems = Numerical().SelectMany(pair => SinglePrecisionProblems(pair.Node, pair.Assembly, seenExemptions)).ToList();

        foreach (var stale in PermittedSinglePrecisionSites.Except(seenExemptions).OrderBy(exemption => exemption.Node, StringComparer.Ordinal))
        {
            problems.Add($"{stale.Node}: {stale.Type}.{stale.Method} is listed in PermittedSinglePrecisionSites, and no such method exists in " +
                         "that node's assembly: the exemption names a site that must be present to be exempted, not a standing permission " +
                         "(remove the entry, or restore the site)");
        }

        Assert.True(problems.Count == 0, "root BOOT.md, Invariants: double precision only.\n" + string.Join("\n", problems));
    }

    [Fact]
    public void OnlyTheExecutionNodeNamesCudaTypes()
    {
        var problems = NodeAssemblies.Assemblies.OrderBy(pair => pair.Key.RelativePath, StringComparer.Ordinal)
            .Where(pair => pair.Key.RelativePath != CudaNode)
            .SelectMany(pair => CudaProblems(pair.Key, pair.Value))
            .ToList();
        Assert.True(problems.Count == 0, "root BOOT.md, Taboos: no CUDA type outside Execution.\n" + string.Join("\n", problems.Distinct()));
    }

    [Fact]
    public void NumericalNodesHaveNoMutableStaticField()
    {
        var problems = Numerical().SelectMany(pair => MutableStaticFieldProblems(pair.Node, pair.Assembly)).ToList();
        Assert.True(problems.Count == 0, "root BOOT.md, Invariants: no hidden state.\n" + string.Join("\n", problems));
    }

    private static IEnumerable<(Node Node, Assembly Assembly)> Numerical()
    {
        foreach (var path in NumericalNodes)
        {
            var node = Tree.Nodes.SingleOrDefault(candidate => candidate.RelativePath == path)
                       ?? throw new InvalidOperationException($"{path} is not a node of the tree; the list of numerical nodes is stale");
            yield return (node, NodeAssemblies.Assemblies[node]);
        }
    }

    /// <summary>Walks one numerical node's assembly for single-precision problems, and records into
    /// <paramref name="seenExemptions"/> which entry of <see cref="PermittedSinglePrecisionSites"/>, if any, a
    /// real method body matched — the caller uses that to catch a stale (site removed or renamed) exemption
    /// entry, which would otherwise leave this fact vacuously green.</summary>
    private static IEnumerable<string> SinglePrecisionProblems(Node node, Assembly assembly, HashSet<SinglePrecisionExemption> seenExemptions)
    {
        foreach (var type in assembly.GetTypes())
        {
            foreach (var (where, referenced) in TypeShape.Shape(type))
            {
                if (IsSinglePrecision(referenced))
                {
                    yield return $"{node.Name}: {type.FullName}, {where} is {referenced.Name}";
                }
            }

            foreach (var method in TypeShape.MethodsOf(type))
            {
                var exemption = PermittedSinglePrecisionSites.SingleOrDefault(
                    candidate => candidate.Node == node.RelativePath && candidate.Type == type.FullName && candidate.Method == method.Name);

                // Not pre-`Distinct()`ed: the exempted branch below counts occurrences of `conv.r4`, not merely
                // its presence, so that a second one added to the same method is still visible as a count of 2.
                var opcodes = IlBody.Instructions(method).Select(instruction => instruction.Code.Name!).Where(name => name.Contains(".r4", StringComparison.Ordinal)).ToList();

                if (exemption is not null)
                {
                    _ = seenExemptions.Add(exemption);
                    var convR4Count = opcodes.Count(name => name == "conv.r4");
                    var otherOpcodes = opcodes.Where(name => name != "conv.r4").Distinct().ToList();
                    if (convR4Count != exemption.ConvR4Count || otherOpcodes.Count > 0)
                    {
                        yield return $"{node.Name}: {type.FullName}.{method.Name} is the declared single-precision exemption (root BOOT.md, " +
                                     $"Taboos), permitted exactly {exemption.ConvR4Count} `conv.r4` and nothing else, but has {convR4Count} " +
                                     "`conv.r4`" + (otherOpcodes.Count > 0 ? $" and also ({string.Join(", ", otherOpcodes)})" : "") +
                                     " — the exemption covers exactly the declared accumulator rounding, nothing more";
                    }
                }
                else if (opcodes.Count > 0)
                {
                    yield return $"{node.Name}: {type.FullName}.{method.Name} performs single-precision operations ({string.Join(", ", opcodes.Distinct())})";
                }

                // Not exempted even for the exempted method: the taboo's carve-out is the one rounding
                // conversion, not a licence for this method to bind to `float` any other way.
                var boundSingle = IlBody.BoundTypes(method).Where(IsSinglePrecision).Select(bound => bound.Name).Distinct().ToList();
                if (boundSingle.Count > 0)
                {
                    yield return $"{node.Name}: {type.FullName}.{method.Name} binds to a single-precision type ({string.Join(", ", boundSingle)})";
                }
            }
        }
    }

    private static IEnumerable<string> CudaProblems(Node node, Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            foreach (var referenced in TypeShape.ReferencedTypes(type))
            {
                if (referenced.Namespace is { } ns && CudaNamespaces.Any(cuda => ns == cuda || ns.StartsWith(cuda + ".", StringComparison.Ordinal)))
                {
                    yield return $"{node.Name}: {type.FullName} names {referenced.FullName}";
                }
            }
        }
    }

    private static IEnumerable<string> MutableStaticFieldProblems(Node node, Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (TypeShape.IsCompilerGenerated(type))
            {
                continue;
            }

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (field.IsLiteral || TypeShape.IsCompilerGenerated(field))
                {
                    continue;
                }

                if (!field.IsInitOnly)
                {
                    yield return $"{node.Name}: {type.FullName}.{field.Name} is a static field that is neither const nor readonly";
                }
                else if (field.FieldType.IsArray)
                {
                    yield return $"{node.Name}: {type.FullName}.{field.Name} is a static readonly array, whose elements are mutable state";
                }
            }
        }
    }

    /// <summary>Whether a type is, or is built from, <c>float</c> or <c>Half</c> anywhere in its shape (root
    /// BOOT.md, Invariants: double precision only): the same unwrap <see cref="TypeShape.ReferencedTypes"/>
    /// uses (<see cref="TypeShape.Unwrap"/>), so a nested array, a by-reference-to-array parameter
    /// (<c>out float[]</c>) or a generic argument buried behind either cannot slip past a single, one-layer copy
    /// of the walk.</summary>
    private static bool IsSinglePrecision(Type type) => TypeShape.Unwrap(type).Any(bare => bare == typeof(float) || bare == typeof(Half));
}

/// <summary>One entry of <see cref="InvariantTests.PermittedSinglePrecisionSites"/>: a single method body, named
/// by its node's <see cref="Node.RelativePath"/>, its declaring type's <see cref="Type.FullName"/> and its own
/// name, that the root's Taboos let hold exactly <paramref name="ConvR4Count"/> `conv.r4` instructions and
/// nothing else single-precision.</summary>
internal sealed record SinglePrecisionExemption(string Node, string Type, string Method, int ConvR4Count);
