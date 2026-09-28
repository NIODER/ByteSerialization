namespace MessagingByteSerialization.Generators.Model;

/// <summary>
/// Every fixed-size primitive supported directly on the wire. The name of each member matches, on
/// purpose, both a <c>WellKnownTypeCodes</c> constant name in the Abstractions assembly and (for the
/// numeric kinds) the suffix of the corresponding <c>System.Buffers.Binary.BinaryPrimitives</c> methods,
/// so the emitter can build both identifiers by string interpolation instead of a second lookup table.
/// </summary>
internal enum PrimitiveKind
{
    Boolean,
    Byte,
    SByte,
    Int16,
    UInt16,
    Int32,
    UInt32,
    Int64,
    UInt64,
    Single,
    Double,
    Char,
    Decimal,
    Guid,
    DateTime,
    TimeOnly,
}

internal static class PrimitiveKindExtensions
{
    public static int Size(this PrimitiveKind kind) => kind switch
    {
        PrimitiveKind.Boolean => sizeof(bool),
        PrimitiveKind.Byte => sizeof(byte),
        PrimitiveKind.SByte => sizeof(sbyte),
        PrimitiveKind.Int16 => sizeof(short),
        PrimitiveKind.UInt16 => sizeof(ushort),
        PrimitiveKind.Int32 => sizeof(int),
        PrimitiveKind.UInt32 => sizeof(uint),
        PrimitiveKind.Int64 => sizeof(long),
        PrimitiveKind.UInt64 => sizeof(ulong),
        PrimitiveKind.Single => sizeof(float),
        PrimitiveKind.Double => sizeof(double),
        PrimitiveKind.Char => sizeof(char),
        PrimitiveKind.Decimal => sizeof(decimal),
        PrimitiveKind.Guid => 16,
        PrimitiveKind.DateTime => sizeof(long), // encoded via DateTime.ToBinary()/FromBinary(long)
        PrimitiveKind.TimeOnly => sizeof(long), // encoded via TimeOnly.Ticks
        _ => 0,
    };

    /// <summary>Name of the matching constant on <c>MessagingByteSerialization.WellKnownTypeCodes</c>.</summary>
    public static string WellKnownCodeConstantName(this PrimitiveKind kind) => kind.ToString();
}
