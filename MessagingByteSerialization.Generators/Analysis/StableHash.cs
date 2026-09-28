using System.Text;

namespace MessagingByteSerialization.Generators.Analysis;

/// <summary>
/// Computes the deterministic <see cref="ushort"/> used as a user-defined type's
/// <c>IByteSerializable&lt;TSelf&gt;.TypeHash</c>. The value is derived purely from the type's
/// fully-qualified metadata name, so it is stable across builds and independent of declaration order -
/// a requirement for wire compatibility between processes built from slightly different source snapshots.
/// </summary>
internal static class StableHash
{
    /// <summary>
    /// FNV-1a 32-bit over the UTF8 bytes of <paramref name="fullyQualifiedMetadataName"/>, folded into 16
    /// bits by XOR-ing the high and low halves. Values that would fall into
    /// <see cref="WellKnownTypeCodeNames"/>'s reserved range (see below) are pushed out of it by flipping
    /// the top bit, so user-defined codes can never collide with built-in primitive codes by construction.
    /// Collisions between two different user-defined types are still possible (65504 possible values) and
    /// are caught separately, per-compilation, by the factory emitter.
    /// </summary>
    public static ushort Compute(string fullyQualifiedMetadataName)
    {
        unchecked
        {
            const uint offsetBasis = 2166136261;
            const uint prime = 16777619;

            uint hash = offsetBasis;
            byte[] bytes = Encoding.UTF8.GetBytes(fullyQualifiedMetadataName);

            for (int i = 0; i < bytes.Length; i++)
            {
                hash ^= bytes[i];
                hash *= prime;
            }

            ushort folded = (ushort)((hash >> 16) ^ (hash & 0xFFFF));

            const ushort reservedRangeMax = 32; // must match WellKnownTypeCodes.ReservedRangeMax

            if (folded <= reservedRangeMax)
            {
                folded = (ushort)(folded ^ 0x8000);
            }

            return folded;
        }
    }
}
