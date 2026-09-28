using System;

namespace MessagingByteSerialization;

/// <summary>
/// Marks a field or property to be serialized as <typeparamref name="TTargetType"/> instead of its own
/// declared type. The declared type must have an explicit or implicit conversion to
/// <typeparamref name="TTargetType"/> (used when writing) and one back from
/// <typeparamref name="TTargetType"/> (used when reading), and <typeparamref name="TTargetType"/> itself
/// must be a type the generator otherwise supports.
///
/// Example: a property of a type without a "natural" wire representation can piggyback on one that has,
/// as long as both conversions exist:
/// <code>
/// [ByteSerializableAs&lt;string&gt;]
/// public SomeType Prop { get; set; } // SomeType must define casts to/from string
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class ByteSerializableAsAttribute<TTargetType> : Attribute
{
}
