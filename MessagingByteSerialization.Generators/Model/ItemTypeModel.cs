namespace MessagingByteSerialization.Generators.Model;

internal enum ItemShape
{
    Scalar,
    List,
    Array,
}

/// <summary>
/// The type of a single List/array element. Usually a leaf <see cref="ScalarTypeModel"/> (fixed primitive,
/// enum, nested [ByteSerializable] type, or string), but may itself be <see cref="ItemShape.List"/> or
/// <see cref="ItemShape.Array"/> to support nested collections - List&lt;List&lt;T&gt;&gt;, T[][], and
/// arbitrary combinations/depths thereof (List&lt;T[]&gt;, T[][][], ...). Each nested collection item
/// is self-delimiting on the wire (it carries its own [kind][item type code][count] header), so nesting
/// composes for free once read/write/size all recurse through this type.
///
/// Dictionary keys/values are intentionally NOT extended this way and stay plain <see cref="ScalarTypeModel"/> -
/// only List/array elements support nesting, matching what was actually asked for.
/// </summary>
internal sealed class ItemTypeModel
{
    public ItemShape Shape { get; init; }

    /// <summary>Valid when <see cref="Shape"/> is <see cref="ItemShape.Scalar"/>.</summary>
    public ScalarTypeModel? Scalar { get; init; }

    /// <summary>Valid when <see cref="Shape"/> is <see cref="ItemShape.List"/> or <see cref="ItemShape.Array"/>: this item's own item type.</summary>
    public ItemTypeModel? Element { get; init; }

    /// <summary>List/array items are always variable-size on the wire (they carry their own header), even when their own nested items are fixed-size.</summary>
    public bool IsFixedSize => Shape == ItemShape.Scalar && Scalar!.IsFixedSize;

    public int FixedSize => Shape == ItemShape.Scalar ? Scalar!.FixedSize : 0;
}
