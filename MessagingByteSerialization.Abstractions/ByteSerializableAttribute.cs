using System;

namespace MessagingByteSerialization;

/// <summary>
/// Marks a partial class, struct or record as a target for byte serialization code generation.
/// The generator implements <see cref="IByteSerializable{TSelf}"/> on the decorated type.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class ByteSerializableAttribute : Attribute
{
}
