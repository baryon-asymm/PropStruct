using System.Reflection;
using System.Reflection.Emit;

namespace PropStruct.Protocol.Tests;

/// <summary>
/// The instructions of a method body, and the types they bind to. The operand width of every opcode is read
/// from the runtime's own table (<see cref="OpCodes"/>), not transcribed: a wrong width would desynchronise the
/// walk and yield garbage instead of failing (AGENTS.md §13: dependencies are read from method bodies too, and
/// a check that reads them wrong must fail loudly rather than pass everything by accident). Takes methods,
/// never types: a type's own methods are <see cref="TypeShape"/>'s to enumerate, so that
/// <c>TypeShape</c> → <c>IlBody</c> stays one-way.
/// </summary>
internal static class IlBody
{
    private static readonly Dictionary<byte, OpCode> OneByte = OpCodeTable(single: true);

    private static readonly Dictionary<byte, OpCode> TwoByte = OpCodeTable(single: false);

    /// <summary>Every <see cref="OperandType"/> whose width does not depend on the instruction stream
    /// (<see cref="OperandType.InlineSwitch"/> is the one exception, handled separately below), keyed by the
    /// enum itself rather than switched on: a lookup carries no unreachable branch and never has to name
    /// <see cref="OperandType.InlinePhi"/>, which is <see langword="obsolete"/> — CS0618 (a warning,
    /// TreatWarningsAsErrors) forbids referencing it by name. A type not listed here (InlinePhi included) gets
    /// the fallback width below, 4, exactly as it did before this table existed.</summary>
    private static readonly Dictionary<OperandType, int> OperandWidths = new()
    {
        [OperandType.InlineNone] = 0,
        [OperandType.ShortInlineBrTarget] = 1,
        [OperandType.ShortInlineI] = 1,
        [OperandType.ShortInlineVar] = 1,
        [OperandType.InlineVar] = 2,
        [OperandType.InlineI8] = 8,
        [OperandType.InlineR] = 8,
    };

    /// <summary>The instructions of a method body, empty for one the runtime cannot report a body for.</summary>
    public static IEnumerable<Instruction> Instructions(MethodBase method)
    {
        var il = Body(method)?.GetILAsByteArray() ?? [];
        var typeContext = method.DeclaringType is { IsGenericTypeDefinition: true } declaring ? declaring.GetGenericArguments() : null;
        var methodContext = method.IsGenericMethodDefinition ? method.GetGenericArguments() : null;
        var module = method.Module;
        var offset = 0;
        while (offset < il.Length)
        {
            var code = il[offset++];
            OpCode opcode;
            if (code == 0xFE)
            {
                if (offset >= il.Length || !TwoByte.TryGetValue(il[offset++], out opcode))
                {
                    yield break;
                }
            }
            else if (!OneByte.TryGetValue(code, out opcode))
            {
                yield break;
            }

            var width = opcode.OperandType == OperandType.InlineSwitch
                ? 4 + 4 * BitConverter.ToInt32(il, offset)
                : OperandWidths.GetValueOrDefault(opcode.OperandType, 4);

            MemberInfo? operand = null;
            if (opcode.OperandType is OperandType.InlineMethod or OperandType.InlineField or OperandType.InlineType or OperandType.InlineTok)
            {
                try
                {
                    operand = module.ResolveMember(BitConverter.ToInt32(il, offset), typeContext, methodContext);
                }
                catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or NotSupportedException)
                {
                    // A token this context cannot resolve names nothing attributable to a node; the walk stays in step either way.
                }
            }

            offset += width;
            yield return new Instruction(opcode, operand);
        }
    }

    /// <summary>
    /// The types a method's body binds to: the members its instructions name, with the types those members'
    /// signatures carry and the generic arguments of the methods it calls. The signatures matter: an enum used
    /// only through its literals is an integer in the IL and appears in no token of its own, yet the property it
    /// is assigned to names it, and that is where the use is caught.
    /// </summary>
    public static IEnumerable<Type> BoundTypes(MethodBase method)
    {
        foreach (var instruction in Instructions(method))
        {
            foreach (var type in Bound(instruction.Operand))
            {
                yield return type;
            }
        }
    }

    /// <summary>The local variables declared in a method body, empty for one the runtime cannot report a body for.</summary>
    public static IEnumerable<LocalVariableInfo> Locals(MethodBase method) => Body(method)?.LocalVariables ?? [];

    /// <summary>Not a <c>switch</c>: <c>Type</c>/<c>MethodBase</c>/<c>FieldInfo</c> are each a subtype of the
    /// final, catch-all case, <c>MemberInfo</c> itself, so a <c>default:</c> arm naming that already-exhaustive
    /// coverage is unreachable code (CS0162, a warning, TreatWarningsAsErrors) — while omitting it left the
    /// switch unpopulated in the analyzer's own reading (IDE0072, the twin of IDE0060 rather than a further
    /// direction to flip: root BOOT.md, "Language and build", decided 2026-09-25). An <c>if</c>/<c>else if</c>
    /// chain carries neither rule: a plain pattern match, not a switch, so IDE0072 does not apply, and the last
    /// arm simply falls through when <paramref name="operand"/> is <see langword="null"/>, exactly as
    /// <c>case null: break;</c> did.</summary>
    private static IEnumerable<Type> Bound(MemberInfo? operand)
    {
        if (operand is Type named)
        {
            yield return named;
        }
        else if (operand is MethodBase called)
        {
            foreach (var type in BoundToCall(called))
            {
                yield return type;
            }
        }
        else if (operand is FieldInfo field)
        {
            if (field.DeclaringType is { } holder)
            {
                yield return holder;
            }

            yield return field.FieldType;
        }
        else if (operand is MemberInfo member)
        {
            if (member.DeclaringType is { } declaring)
            {
                yield return declaring;
            }
        }
    }

    private static IEnumerable<Type> BoundToCall(MethodBase called)
    {
        if (called.DeclaringType is { } owner)
        {
            yield return owner;
        }

        if (called is MethodInfo { IsGenericMethod: true } generic)
        {
            foreach (var argument in generic.GetGenericArguments())
            {
                yield return argument;
            }
        }

        if (called is MethodInfo info)
        {
            yield return info.ReturnType;
        }

        foreach (var parameter in called.GetParameters())
        {
            yield return parameter.ParameterType;
        }
    }

    private static MethodBody? Body(MethodBase method)
    {
        try
        {
            return method.GetMethodBody();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or NotSupportedException)
        {
            return null;
        }
    }

    private static Dictionary<byte, OpCode> OpCodeTable(bool single)
    {
        var table = new Dictionary<byte, OpCode>();
        foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is not OpCode opcode)
            {
                continue;
            }

            var value = (ushort)opcode.Value;
            if (single ? value <= 0xFF && opcode.Value != 0xFE : value > 0xFF)
            {
                table[(byte)(value & 0xFF)] = opcode;
            }
        }

        return table;
    }
}
