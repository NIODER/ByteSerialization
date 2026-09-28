namespace MessagingByteSerialization;

/// <summary>
/// Reserved <see cref="IByteSerializable{TSelf}.TypeHash"/> values for built-in fixed-size primitive types.
/// The generator emits references to these constants (never their numeric literals) so the mapping can
/// only ever change in one place. User-defined types are hashed from their fully-qualified name via a
/// generator-internal FNV-1a routine and are guaranteed (by construction, see StableHash) to never land
/// in the <see cref="ReservedRangeMax"/> range reserved here.
/// </summary>
public static class WellKnownTypeCodes
{
    /// <summary>Codes up to and including this value are reserved for built-in types.</summary>
    public const ushort ReservedRangeMax = 32;

    public const ushort Boolean = 1;
    public const ushort Byte = 2;
    public const ushort SByte = 3;
    public const ushort Int16 = 4;
    public const ushort UInt16 = 5;
    public const ushort Int32 = 6;
    public const ushort UInt32 = 7;
    public const ushort Int64 = 8;
    public const ushort UInt64 = 9;
    public const ushort Single = 10;
    public const ushort Double = 11;
    public const ushort Char = 12;
    public const ushort Decimal = 13;
    public const ushort Guid = 14;

    /// <summary>Item type code for List&lt;string&gt;/string[] elements (never used for a top-level string member, which omits the item type code field entirely).</summary>
    public const ushort String = 15;

    /// <summary>Encoded via <see cref="System.DateTime.ToBinary"/>/<see cref="System.DateTime.FromBinary"/> (preserves <see cref="System.DateTimeKind"/>), not raw ticks.</summary>
    public const ushort DateTime = 16;

    /// <summary>Encoded via <see cref="System.TimeOnly.Ticks"/>.</summary>
    public const ushort TimeOnly = 17;

    /// <summary>
    /// Item type code marking a List/array item that is itself a nested List (e.g. the outer member of
    /// <c>List&lt;List&lt;int&gt;&gt;</c>). The nested item still carries its own full
    /// [kind][item type code][count] header describing its own items - this code only tells the reader
    /// "expect a nested collection here", it does not need to (and cannot, in 2 bytes) describe the leaf type.
    /// </summary>
    public const ushort NestedList = 18;

    /// <summary>Item type code marking a List/array item that is itself a nested array (e.g. <c>int[][]</c>). See <see cref="NestedList"/>.</summary>
    public const ushort NestedArray = 19;
}
