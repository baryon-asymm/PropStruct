using System.Reflection;
using System.Reflection.Emit;
using PropStruct.Particle;
using PropStruct.Random;
using Xunit;

namespace PropStruct.Execution.Tests;

/// <summary>
/// L0 (BOOT.md, Opus audit item 4, 2026-09-18): <see cref="MathProbe.Functions"/> was a hand-typed list of
/// the <see cref="Math"/> members the particle program may call (root BOOT.md, Constraints: "of System.Math
/// only the double overloads Log, Sqrt, Pow, Acos, Sin, Tan, Abs, Max, Min, Floor"), checked by nothing
/// against the actual code: a call to a new <see cref="Math"/> member added inside <c>Particle</c> or
/// <c>Random</c> would compile and run — every kernel-compatible <see cref="Math"/> overload used here has
/// its own CPU intrinsic — without a libdevice wrapper or a probe row ever being added for it, silently
/// leaving CUDA's answer for that one function unchecked against the CPU accelerator (root BOOT.md,
/// Constraints: "each with a libdevice wrapper"). This test disassembles every method body declared by a
/// type of either assembly and asserts every <see cref="Math"/> member call found is named in
/// <see cref="MathProbe.Functions"/>.
/// </summary>
/// <remarks>
/// The decoder below is built entirely from <see cref="OpCodes"/>' own reflection data (byte value to
/// <see cref="OpCode.OperandType"/>), not a hand-transcribed ECMA-335 opcode table: a typo in a hand-written
/// table would silently misalign the scan past the point of the typo, an error more dangerous than a check
/// that is merely absent, since it also stops noticing every call after it. Deriving the table from
/// <see cref="OpCodes"/> itself removes that entire class of mistake.
/// </remarks>
public sealed class MathProbeCoverageTests
{
    [Fact]
    public void EveryMathMemberCalledByParticleOrRandomIsInMathProbeFunctions()
    {
        var uncovered = new List<string>();
        var totalCalls = 0;

        foreach (var assembly in new[] { typeof(Attempt).Assembly, typeof(Mcg128).Assembly })
        {
            foreach (var type in assembly.GetTypes())
            {
                foreach (var method in DeclaredMembersOf(type))
                {
                    foreach (var mathMember in MathMembersCalledBy(method))
                    {
                        totalCalls++;
                        if (!MathProbe.Functions.Contains(mathMember))
                        {
                            uncovered.Add($"{type.FullName}.{method.Name} calls Math.{mathMember}");
                        }
                    }
                }
            }
        }

        // Guards against a vacuous pass: if the IL decoder found no System.Math calls at all (a bug in the
        // decoder itself, not in the code it scans), "nothing uncovered" would be trivially true. Particle
        // alone calls at least Log, Sqrt and Pow inside the neighbour loop and the size draw (root BOOT.md,
        // Constraints), so a real scan must find more than a handful.
        Assert.True(totalCalls >= 10, $"the IL decoder found only {totalCalls} System.Math call(s) across both assemblies; that is too few to trust an empty uncovered list.");

        Assert.True(uncovered.Count == 0,
            "System.Math member(s) called but not in MathProbe.Functions (root BOOT.md needs the member added to its own list, a libdevice wrapper and a probe row):\n"
            + string.Join("\n", uncovered));
    }

    private static IEnumerable<MethodBase> DeclaredMembersOf(Type type)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        foreach (var method in type.GetMethods(flags))
        {
            yield return method;
        }

        foreach (var constructor in type.GetConstructors(flags))
        {
            yield return constructor;
        }

        if (type.TypeInitializer is not null)
        {
            yield return type.TypeInitializer;
        }
    }

    private static IEnumerable<string> MathMembersCalledBy(MethodBase method)
    {
        var body = method.GetMethodBody();
        var il = body?.GetILAsByteArray();
        if (il is null)
        {
            yield break;
        }

        var module = method.Module;
        var typeArguments = method.DeclaringType is { IsGenericType: true } declaring ? declaring.GetGenericArguments() : null;
        var methodArguments = method.IsGenericMethod ? method.GetGenericArguments() : null;

        var i = 0;
        while (i < il.Length)
        {
            short opValue;
            int opSize;
            if (il[i] == 0xFE)
            {
                opValue = (short)(0xFE00 | il[i + 1]);
                opSize = 2;
            }
            else
            {
                opValue = il[i];
                opSize = 1;
            }

            if (!OpCodesByValue.TryGetValue(opValue, out var opcode))
            {
                throw new InvalidOperationException(
                    $"{method.DeclaringType}.{method.Name}: unrecognised IL opcode 0x{opValue:X} at offset {i} (byte {i - opSize + 1} of {il.Length}).");
            }

            i += opSize;

            if (opcode.OperandType == OperandType.InlineSwitch)
            {
                var caseCount = BitConverter.ToInt32(il, i);
                i += 4 + caseCount * 4;
                continue;
            }

            if (opcode.OperandType == OperandType.InlineMethod)
            {
                var token = BitConverter.ToInt32(il, i);
                MethodBase? resolved = null;
                try
                {
                    resolved = module.ResolveMethod(token, typeArguments, methodArguments);
                }
                catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or NotSupportedException)
                {
                    // A token this decoder's guessed generic context cannot resolve is not a Math call
                    // either way; skip it rather than abort the whole assembly's scan over one method.
                }

                if (resolved?.DeclaringType == typeof(Math))
                {
                    yield return resolved.Name;
                }
            }

            i += OperandSize(opcode.OperandType);
        }
    }

    /// <summary>Every <see cref="OperandType"/> this probe knows how to skip past, keyed by the enum itself
    /// rather than switched on: a lookup carries no unreachable branch and never has to name
    /// <see cref="OperandType.InlinePhi"/>, which is <see langword="obsolete"/> — CS0618 (a warning,
    /// TreatWarningsAsErrors) forbids referencing it by name. Neither <see cref="OperandType.InlinePhi"/> nor
    /// <see cref="OperandType.InlineSwitch"/> (a variable-length operand this probe never needs to size) is
    /// listed, so both still reach <see cref="OperandSize"/>'s own fallback exception, exactly as before this
    /// table existed.</summary>
    private static readonly Dictionary<OperandType, int> KnownOperandSizes = new()
    {
        [OperandType.InlineNone] = 0,
        [OperandType.ShortInlineBrTarget] = 1,
        [OperandType.ShortInlineI] = 1,
        [OperandType.ShortInlineVar] = 1,
        [OperandType.InlineVar] = 2,
        [OperandType.InlineBrTarget] = 4,
        [OperandType.InlineField] = 4,
        [OperandType.InlineI] = 4,
        [OperandType.InlineMethod] = 4,
        [OperandType.InlineSig] = 4,
        [OperandType.InlineString] = 4,
        [OperandType.InlineTok] = 4,
        [OperandType.InlineType] = 4,
        [OperandType.ShortInlineR] = 4,
        [OperandType.InlineI8] = 8,
        [OperandType.InlineR] = 8,
    };

    private static int OperandSize(OperandType type) =>
        KnownOperandSizes.TryGetValue(type, out var size)
            ? size
            : throw new InvalidOperationException($"Unhandled IL operand type {type}: the decoder cannot skip past it correctly.");

    /// <summary>Every <see cref="OpCode"/> the runtime itself declares, keyed by its encoded byte value
    /// (<c>0xFEnn</c> for a two-byte opcode) — built from <see cref="OpCodes"/>' own fields, not transcribed.</summary>
    private static readonly Dictionary<short, OpCode> OpCodesByValue = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.FieldType == typeof(OpCode))
        .Select(field => (OpCode)field.GetValue(null)!)
        .ToDictionary(opcode => opcode.Value, opcode => opcode);
}
