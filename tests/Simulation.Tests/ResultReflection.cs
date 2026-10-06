using System.Reflection;
using Xunit;

namespace PropStruct.Simulation.Tests;

/// <summary>
/// Reflection helpers shared by the L0 "every field of <c>CycleReport</c>" check and the L1 bit-for-bit
/// comparison of two <see cref="SimulationResult"/>s. Kept in one place so both tests walk the same object
/// graph the same way (AGENTS.md §6: a criterion with the quantifier "all" is checked against a
/// machine-generated list, not one typed by hand).
/// </summary>
internal static class ResultReflection
{
    /// <summary>
    /// Every property name reachable from <typeparamref name="T"/>, recursing into properties whose type is
    /// itself a type of this node (a member record of <see cref="SimulationResult"/>) and stopping at
    /// everything else (a <see langword="double"/>, an <c>ImmutableArray&lt;double&gt;</c>, an
    /// <c>AcceleratorInfo</c> from <c>Execution</c>, ...), which is a leaf for this purpose.
    /// </summary>
    public static HashSet<string> LeafPropertyNames<T>() => LeafPropertyNames(typeof(T), new HashSet<Type>());

    private static HashSet<string> LeafPropertyNames(Type type, HashSet<Type> visited)
    {
        var names = new HashSet<string>();
        if (!visited.Add(type))
        {
            return names;
        }

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            _ = names.Add(property.Name);
            var propertyType = property.PropertyType;
            if (propertyType.Namespace == typeof(SimulationResult).Namespace && propertyType.IsClass)
            {
                names.UnionWith(LeafPropertyNames(propertyType, visited));
            }
        }

        return names;
    }

    /// <summary>Every property name of <c>Statistics.CycleReport</c> (internal, reachable via <c>InternalsVisibleTo</c>).</summary>
    public static HashSet<string> CycleReportFieldNames()
    {
        var type = typeof(Statistics.Setup).Assembly.GetType("PropStruct.Statistics.CycleReport")
            ?? throw new InvalidOperationException("PropStruct.Statistics.CycleReport not found by reflection.");
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name).ToHashSet();
    }

    /// <summary>
    /// Every numeric leaf of <paramref name="expected"/> and <paramref name="actual"/> compared exactly:
    /// <see langword="double"/> by its bit pattern (so two <see cref="double.NaN"/>s of the same run compare
    /// equal, matching the bit-identical results root BOOT.md's determinism invariant requires), everything else by
    /// <see cref="object.Equals(object?, object?)"/>, arrays and <c>ImmutableArray&lt;T&gt;</c> element-wise,
    /// <c>double[,]</c> cell-wise. Recurses into member records the same way <see cref="LeafPropertyNames{T}"/> does.
    /// </summary>
    public static void AssertBitIdentical(object? expected, object? actual, string path = "")
    {
        if (expected is null || actual is null)
        {
            Assert.True(expected is null && actual is null, $"{path}: one of the two is null.");
            return;
        }

        var type = expected.GetType();
        Assert.Equal(type, actual.GetType());

        if (type == typeof(double))
        {
            var a = (double)expected;
            var b = (double)actual;
            Assert.True(BitConverter.DoubleToInt64Bits(a) == BitConverter.DoubleToInt64Bits(b), $"{path}: {a:R} != {b:R}");
            return;
        }

        if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(TimeSpan))
        {
            Assert.True(Equals(expected, actual), $"{path}: {expected} != {actual}");
            return;
        }

        if (expected is double[,] matrixExpected && actual is double[,] matrixActual)
        {
            Assert.Equal(matrixExpected.GetLength(0), matrixActual.GetLength(0));
            Assert.Equal(matrixExpected.GetLength(1), matrixActual.GetLength(1));
            for (var i = 0; i < matrixExpected.GetLength(0); i++)
            {
                for (var j = 0; j < matrixExpected.GetLength(1); j++)
                {
                    var a = matrixExpected[i, j];
                    var b = matrixActual[i, j];
                    Assert.True(BitConverter.DoubleToInt64Bits(a) == BitConverter.DoubleToInt64Bits(b), $"{path}[{i},{j}]: {a:R} != {b:R}");
                }
            }

            return;
        }

        if (expected is System.Collections.IEnumerable enumerableExpected && actual is System.Collections.IEnumerable enumerableActual
            && type != typeof(string))
        {
            var expectedList = enumerableExpected.Cast<object?>().ToList();
            var actualList = enumerableActual.Cast<object?>().ToList();
            Assert.True(expectedList.Count == actualList.Count, $"{path}: length {expectedList.Count} != {actualList.Count}");
            for (var i = 0; i < expectedList.Count; i++)
            {
                AssertBitIdentical(expectedList[i], actualList[i], $"{path}[{i}]");
            }

            return;
        }

        if (type.Namespace == typeof(SimulationResult).Namespace && type.IsClass)
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                AssertBitIdentical(property.GetValue(expected), property.GetValue(actual), $"{path}.{property.Name}");
            }

            return;
        }

        Assert.True(Equals(expected, actual), $"{path}: {expected} != {actual}");
    }
}
