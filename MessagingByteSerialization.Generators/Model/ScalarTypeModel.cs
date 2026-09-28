namespace MessagingByteSerialization.Generators.Model;

internal enum ScalarKind
{
    FixedPrimitive,
    Enum,
    NestedByteSerializable,

    /// <summary>Only ever produced as a collection element (List&lt;string&gt;/string[]) - a top-level string
    /// member uses <see cref="MemberShape.String"/> instead. Self-delimiting: each item is written as
    /// [4-byte UTF8 byte count][UTF8 bytes], with no repeated CollectionKind marker per item.</summary>
    String,
}

/// <summary>
/// Describes a single non-collection value: a fixed-size primitive, an enum (serialized as its underlying
/// primitive), another <c>[ByteSerializable]</c> type, or (element position only) a string. Used both for
/// plain members and for the element type of List/array members - collections of collections are not
/// supported.
/// </summary>
internal sealed class ScalarTypeModel
{
    public ScalarKind Kind { get; init; }

    /// <summary>Fully-qualified, globally-rooted display name of the member/element's own type.</summary>
    public string TypeDisplayName { get; init; } = "";

    /// <summary>Valid for <see cref="ScalarKind.FixedPrimitive"/> and <see cref="ScalarKind.Enum"/> (underlying type).</summary>
    public PrimitiveKind Primitive { get; init; }

    /// <summary>Valid for <see cref="ScalarKind.NestedByteSerializable"/>: fully-qualified name of the nested type.</summary>
    public string? NestedTypeFullName { get; init; }

    /// <summary>Valid for <see cref="ScalarKind.NestedByteSerializable"/>.</summary>
    public bool NestedIsFixedSize { get; init; }

    /// <summary>Valid for <see cref="ScalarKind.NestedByteSerializable"/> when <see cref="NestedIsFixedSize"/> is true.</summary>
    public int NestedFixedSize { get; init; }

    public bool IsFixedSize => Kind switch
    {
        ScalarKind.FixedPrimitive => true,
        ScalarKind.Enum => true,
        ScalarKind.NestedByteSerializable => NestedIsFixedSize,
        ScalarKind.String => false,
        _ => false,
    };

    public int FixedSize => Kind switch
    {
        ScalarKind.FixedPrimitive => Primitive.Size(),
        ScalarKind.Enum => Primitive.Size(),
        ScalarKind.NestedByteSerializable => NestedFixedSize,
        _ => 0,
    };
}
