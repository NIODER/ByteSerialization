using System;

namespace MessagingByteSerialization;

/// <summary>
/// Marks a field or property to be excluded from serialization entirely: <c>ToBytes</c> writes nothing
/// for it, <c>GetByteSize</c> counts nothing for it, and <c>FromBytes</c> assigns it <c>default</c> rather
/// than reading anything from the wire. The member's own type is never inspected, so it does not need to
/// be a supported (or even serializable) type.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class ByteSerializableIgnoreAttribute : Attribute
{
}
