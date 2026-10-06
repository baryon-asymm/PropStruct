using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Execution.Tests;

/// <summary>
/// The one xunit collection of the <c>Category=Long</c> classes of this assembly (BOOT.md, "## Invariants"):
/// xunit runs the classes of different collections in parallel, and a collection defined with
/// <see cref="CollectionDefinitionAttribute.DisableParallelization"/> runs alone, its classes one after
/// another. The collection's name is <see cref="Name"/>; the definition sits on this class, the one that
/// also guards it, because a dedicated empty marker class would have to be public for xUnit1027 and is
/// never used outside the assembly, which CA1515 rejects (the same resolution as
/// <c>tests/Simulation.Tests/CudaRefusalTests.cs</c>).
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class LongClassesSerialTests(ITestOutputHelper output)
{
    /// <summary>The collection every class with a <c>Long</c> trait names in <c>[Collection]</c>.</summary>
    public const string Name = "ExecutionLongSerial";

    private const string CategoryKey = "Category";
    private const string LongValue = "Long";

    [Fact]
    public void EveryClassWithALongTraitIsInTheSerialCollection()
    {
        var longClasses = typeof(LongClassesSerialTests).Assembly.GetTypes()
            .Where(HasLongTrait)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToList();

        output.WriteLine($"{longClasses.Count} classes with a Long trait: {string.Join(", ", longClasses.Select(type => type.Name))}");
        Assert.NotEmpty(longClasses);

        var outside = longClasses
            .Where(type => !IsInCollection(type))
            .Select(type => type.FullName)
            .ToList();

        Assert.True(outside.Count == 0,
            $"Of {longClasses.Count} classes with a Long trait, these are not in collection '{Name}': {string.Join(", ", outside)}");
    }

    private const BindingFlags AllDeclaredMethods =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static bool HasLongTrait(Type type) =>
        IsLong(CustomAttributeData.GetCustomAttributes(type))
        || type.GetMethods(AllDeclaredMethods).Any(method => IsLong(CustomAttributeData.GetCustomAttributes(method)));

    private static bool IsInCollection(Type type) =>
        CustomAttributeData.GetCustomAttributes(type).Any(attribute =>
            attribute.AttributeType == typeof(CollectionAttribute)
            && attribute.ConstructorArguments.Count == 1
            && Equals(attribute.ConstructorArguments[0].Value, Name));

    private static bool IsLong(IEnumerable<CustomAttributeData> attributes) =>
        attributes.Any(attribute =>
            attribute.AttributeType == typeof(TraitAttribute)
            && attribute.ConstructorArguments.Count == 2
            && Equals(attribute.ConstructorArguments[0].Value, CategoryKey)
            && Equals(attribute.ConstructorArguments[1].Value, LongValue));
}
