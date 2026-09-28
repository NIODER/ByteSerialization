namespace MessagingByteSerialization.Generators.Model;

internal enum MemberShape
{
    Scalar,
    List,
    Array,
    String,
    Dictionary,
}

/// <summary>One serializable field or auto-property, in declaration order.</summary>
internal sealed class MemberModel
{
    public string Name { get; init; } = "";

    public MemberShape Shape { get; init; }

    /// <summary>Valid when <see cref="Shape"/> is <see cref="MemberShape.Scalar"/>.</summary>
    public ScalarTypeModel? Scalar { get; init; }

    /// <summary>Item type, possibly itself a nested List/array. Valid when <see cref="Shape"/> is <see cref="MemberShape.List"/> or <see cref="MemberShape.Array"/>.</summary>
    public ItemTypeModel? Element { get; init; }

    /// <summary>
    /// Key type. Valid when <see cref="Shape"/> is <see cref="MemberShape.Dictionary"/>. Always scalar -
    /// no List/T[]/Dictionary keys, and (unlike List/array items and dictionary values) a nested
    /// [ByteSerializable] key must be fixed-size (see <see cref="Analysis.TypeAnalyzer"/>). String is the
    /// most "variable" a key is allowed to be.
    /// </summary>
    public ScalarTypeModel? Key { get; init; }

    /// <summary>Value type, possibly itself a nested List/array (Dictionary&lt;K, List&lt;V&gt;&gt;, Dictionary&lt;K, V[]&gt;). Valid when <see cref="Shape"/> is <see cref="MemberShape.Dictionary"/>.</summary>
    public ItemTypeModel? DictionaryValue { get; init; }

    /// <summary>
    /// The member's own declared type (fully-qualified display) - what the generated constructor call /
    /// property initializer actually assigns to. Non-null in two cases:
    /// <list type="bullet">
    /// <item>The member is annotated <c>[ByteSerializableAs&lt;TTarget&gt;]</c>: <see cref="Shape"/>/
    /// <see cref="Scalar"/>/<see cref="Element"/>/<see cref="Key"/>/<see cref="DictionaryValue"/> describe
    /// TTarget's shape (that's what actually goes on the wire) - the emitter casts the member's value to
    /// TTarget when writing, and casts the deserialized TTarget value back to this type when reading.</item>
    /// <item><see cref="IsIgnored"/> is true: nothing is read/written for this member at all, and the
    /// emitter constructs it as <c>default(OriginalTypeDisplayName)</c>.</item>
    /// </list>
    /// Null when the member is serialized as its own declared type with no special handling, the normal case.
    /// </summary>
    public string? OriginalTypeDisplayName { get; init; }

    /// <summary>TTarget's fully-qualified display name for a <c>[ByteSerializableAs&lt;TTarget&gt;]</c> member. Always null when <see cref="IsIgnored"/> is true.</summary>
    public string? TargetTypeDisplayName { get; init; }

    /// <summary>
    /// True for a <c>[ByteSerializableIgnore]</c> member: excluded from the wire entirely (nothing written,
    /// nothing counted in GetByteSize, nothing read - always constructed as <c>default</c>). When true,
    /// <see cref="Shape"/>/<see cref="Scalar"/>/<see cref="Element"/>/<see cref="Key"/>/<see cref="DictionaryValue"/>/
    /// <see cref="TargetTypeDisplayName"/> are all meaningless and must not be read; only
    /// <see cref="Name"/> and <see cref="OriginalTypeDisplayName"/> are set.
    /// </summary>
    public bool IsIgnored { get; init; }

    /// <summary>True when this member's serialized size cannot be known without inspecting the runtime value. Always false when <see cref="IsIgnored"/> (it contributes nothing to the wire either way).</summary>
    public bool IsVariableSize => IsIgnored
        ? false
        : Shape switch
        {
            MemberShape.Scalar => !Scalar!.IsFixedSize,
            _ => true,
        };
}
