namespace MessagingByteSerialization;

/// <summary>
/// Wire discriminator written as the first byte of every variable-size collection member.
/// List/Array/String: <c>[1 byte kind][2 byte item type code (omitted for String)][4 byte item/char count][data]</c>.
/// Dictionary: <c>[1 byte kind][2 byte key type code][2 byte value type code][4 byte entry count][data]</c>.
/// </summary>
public enum CollectionKind : byte
{
    List = 1,
    Array = 2,
    String = 3,
    Dictionary = 4,
}
