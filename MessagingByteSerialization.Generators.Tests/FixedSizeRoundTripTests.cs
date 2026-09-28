using MessagingByteSerialization.Generators.Tests.Infrastructure;

namespace MessagingByteSerialization.Generators.Tests;

public class FixedSizeRoundTripTests
{
    [Fact]
    public void PositionalRecordStruct_RoundTrips()
    {
        const string source = """
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial record struct Point(int X, int Y);

            public static class Harness
            {
                public static string Verify()
                {
                    var original = new Point(42, -7);
                    byte[] bytes = original.ToBytes();

                    if (bytes.Length != Point.FixedByteSize)
                    {
                        return $"size mismatch: {bytes.Length} vs {Point.FixedByteSize}";
                    }

                    if (Point.FixedByteSize != 8)
                    {
                        return $"unexpected fixed size {Point.FixedByteSize}";
                    }

                    var restored = Point.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (restored.X != 42 || restored.Y != -7) return "value mismatch";
                    if (Point.TypeHash == 0) return "TypeHash was zero";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }

    [Fact]
    public void AllPrimitives_RoundTrip()
    {
        const string source = """
            using System;
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial record AllPrimitives(
                bool Flag,
                byte B,
                sbyte SB,
                short S16,
                ushort U16,
                int I32,
                uint U32,
                long I64,
                ulong U64,
                float F32,
                double F64,
                char C,
                decimal Dec,
                Guid Id,
                DateTime Created,
                TimeOnly Time);

            public static class Harness
            {
                public static string Verify()
                {
                    var original = new AllPrimitives(
                        true, 200, -100, -12345, 54321, -123456789, 3000000000,
                        -1234567890123, 12345678901234567, 1.5f, 2.5d, 'x',
                        123456789.987654321m, Guid.NewGuid(), DateTime.UtcNow, new TimeOnly(13, 45, 30, 123));

                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != AllPrimitives.FixedByteSize) return "size mismatch";

                    var restored = AllPrimitives.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";

                    if (restored.Flag != original.Flag) return "Flag mismatch";
                    if (restored.B != original.B) return "B mismatch";
                    if (restored.SB != original.SB) return "SB mismatch";
                    if (restored.S16 != original.S16) return "S16 mismatch";
                    if (restored.U16 != original.U16) return "U16 mismatch";
                    if (restored.I32 != original.I32) return "I32 mismatch";
                    if (restored.U32 != original.U32) return "U32 mismatch";
                    if (restored.I64 != original.I64) return "I64 mismatch";
                    if (restored.U64 != original.U64) return "U64 mismatch";
                    if (restored.F32 != original.F32) return "F32 mismatch";
                    if (restored.F64 != original.F64) return "F64 mismatch";
                    if (restored.C != original.C) return "C mismatch";
                    if (restored.Dec != original.Dec) return "Dec mismatch";
                    if (restored.Id != original.Id) return "Id mismatch";
                    if (restored.Created != original.Created) return "Created mismatch";
                    if (restored.Created.Kind != original.Created.Kind) return "Created.Kind mismatch";
                    if (restored.Time != original.Time) return "Time mismatch";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }

    [Fact]
    public void Enum_SerializesAsUnderlyingPrimitive()
    {
        const string source = """
            using MessagingByteSerialization;

            namespace TestNs;

            public enum Color : byte { Red = 0, Green = 1, Blue = 2 }

            [ByteSerializable]
            public partial record struct Widget(Color C, int Weight);

            public static class Harness
            {
                public static string Verify()
                {
                    var original = new Widget(Color.Blue, 99);
                    byte[] bytes = original.ToBytes();

                    if (Widget.FixedByteSize != 5) return $"unexpected fixed size {Widget.FixedByteSize}";
                    if (bytes[0] != (byte)Color.Blue) return "enum byte mismatch";

                    var restored = Widget.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (restored.C != Color.Blue || restored.Weight != 99) return "value mismatch";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }

    [Fact]
    public void PlainClassWithParameterlessConstructor_RoundTrips()
    {
        const string source = """
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial class Vector2
            {
                public double X { get; set; }
                public double Y { get; set; }
            }

            public static class Harness
            {
                public static string Verify()
                {
                    var original = new Vector2 { X = 1.5, Y = -2.5 };
                    byte[] bytes = original.ToBytes();
                    var restored = Vector2.FromBytes(bytes, out int read);

                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (restored.X != 1.5 || restored.Y != -2.5) return "value mismatch";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }
}
