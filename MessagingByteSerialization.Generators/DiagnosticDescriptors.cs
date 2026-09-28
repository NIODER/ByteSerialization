using Microsoft.CodeAnalysis;

namespace MessagingByteSerialization.Generators;

internal static class DiagnosticDescriptors
{
    private const string Category = "MessagingByteSerialization";

    public static readonly DiagnosticDescriptor TypeMustBePartial = new(
        "MBS001",
        "Byte-serializable type must be partial",
        "Type '{0}' is marked with [ByteSerializable] but is not declared 'partial'; serialization code cannot be generated for it",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedMemberType = new(
        "MBS002",
        "Unsupported member type",
        "Member '{0}' of type '{1}' has an unsupported type '{2}'; type '{1}' is skipped. Supported member types are: fixed-size primitives (including Guid, DateTime, TimeOnly), enums, other [ByteSerializable] types, List<T>, T[] (T may itself be List<U>/U[], nested arbitrarily deep), Dictionary<TKey, TValue> and string, where TKey/TValue are scalar (fixed primitive, enum, nested [ByteSerializable] type, or string; not further collections).",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NoSuitableConstructor = new(
        "MBS003",
        "No suitable constructor for deserialization",
        "Type '{0}' must either be a positional record whose primary constructor parameters match its serializable members, or expose a public parameterless constructor with settable members; type is skipped",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor GenericTypeNotSupported = new(
        "MBS004",
        "Generic types are not supported",
        "Type '{0}' is generic; [ByteSerializable] does not support open or closed generic type definitions and the type is skipped",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NestedTypeNotSupported = new(
        "MBS005",
        "Nested types are not supported",
        "Type '{0}' is declared inside another type; [ByteSerializable] only supports namespace-level (top-level) type declarations and the type is skipped",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor CircularFixedSizeReference = new(
        "MBS006",
        "Circular reference between fixed-size types",
        "Type '{0}' participates in a reference cycle through its members; this cannot be laid out as a fixed-size structure and the type is skipped",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateTypeHash = new(
        "MBS007",
        "TypeHash collision",
        "Types '{0}' and '{1}' hash to the same TypeHash value ({2}); one of them will not be reachable through ByteSerializableFactory. Rename one of the types to resolve the collision.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DictionaryKeyMustBeFixedSize = new(
        "MBS008",
        "Dictionary key type must be fixed-size",
        "Dictionary key type '{0}' is a variable-size [ByteSerializable] type. Dictionary keys must be a fixed-size primitive, enum, string, or a fixed-size [ByteSerializable] type.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ByteSerializableAsConversionMissing = new(
        "MBS009",
        "ByteSerializableAs requires conversions in both directions",
        "Member '{0}' has type '{1}' and is marked [ByteSerializableAs<{2}>], but '{1}' has no conversion to '{2}' and/or '{2}' has no conversion back to '{1}'. Define both explicit or implicit conversion operators.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ByteSerializableAsTargetUnsupported = new(
        "MBS010",
        "ByteSerializableAs target type is not supported",
        "Member '{0}' is marked with ByteSerializableAs targeting type '{1}'. Type '{1}' is not itself a supported serializable type.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
