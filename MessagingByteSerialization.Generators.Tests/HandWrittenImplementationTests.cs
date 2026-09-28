using MessagingByteSerialization.Generators.Tests.Infrastructure;

namespace MessagingByteSerialization.Generators.Tests;

/// <summary>
/// A type that implements IByteSerializable&lt;TSelf&gt; itself (hand-written, no [ByteSerializable])
/// should be usable as a member/item/dictionary-value type: the generator calls its existing
/// ToBytes/FromBytes/TypeHash directly rather than trying to generate anything for it.
/// </summary>
public class HandWrittenImplementationTests
{
    private const string ManualPointSource = """
        public sealed class ManualPoint : MessagingByteSerialization.IByteSerializable<ManualPoint>
        {
            public int X { get; }
            public int Y { get; }

            public ManualPoint(int x, int y)
            {
                X = x;
                Y = y;
            }

            public static ushort TypeHash => 4242;

            public int GetByteSize() => 8;

            public void ToBytes(System.Span<byte> destination)
            {
                System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(destination, X);
                System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(4), Y);
            }

            public static ManualPoint FromBytes(System.ReadOnlySpan<byte> source, out int bytesRead)
            {
                int x = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(source);
                int y = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(source.Slice(4));
                bytesRead = 8;
                return new ManualPoint(x, y);
            }
        }
        """;

    [Fact]
    public void PlainMember_UsesExistingImplementation()
    {
        string source = $$"""
            using MessagingByteSerialization;

            namespace TestNs;

            {{ManualPointSource}}

            [ByteSerializable]
            public partial record Message(int Id, ManualPoint Location);

            public static class Harness
            {
                public static string Verify()
                {
                    var original = new Message(1, new ManualPoint(3, 4));
                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != original.GetByteSize()) return "size mismatch";

                    var restored = Message.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (restored.Id != 1) return "Id mismatch";
                    if (restored.Location.X != 3 || restored.Location.Y != 4) return "Location mismatch";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }

    [Fact]
    public void ListItem_UsesExistingImplementation()
    {
        string source = $$"""
            using System.Collections.Generic;
            using MessagingByteSerialization;

            namespace TestNs;

            {{ManualPointSource}}

            [ByteSerializable]
            public partial record Message(List<ManualPoint> Points);

            public static class Harness
            {
                public static string Verify()
                {
                    var original = new Message(new List<ManualPoint> { new ManualPoint(1, 1), new ManualPoint(2, 2) });
                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != original.GetByteSize()) return "size mismatch";

                    var restored = Message.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (restored.Points.Count != 2) return "count mismatch";
                    if (restored.Points[1].X != 2 || restored.Points[1].Y != 2) return "value mismatch";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }

    [Fact]
    public void DictionaryValue_UsesExistingImplementation()
    {
        string source = $$"""
            using System.Collections.Generic;
            using MessagingByteSerialization;

            namespace TestNs;

            {{ManualPointSource}}

            [ByteSerializable]
            public partial record Message(Dictionary<string, ManualPoint> Points);

            public static class Harness
            {
                public static string Verify()
                {
                    var original = new Message(new Dictionary<string, ManualPoint> { ["origin"] = new ManualPoint(0, 0) });
                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != original.GetByteSize()) return "size mismatch";

                    var restored = Message.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (!restored.Points.TryGetValue("origin", out var p) || p.X != 0 || p.Y != 0) return "value mismatch";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }

    [Fact]
    public void DictionaryKey_IsRejectedAsVariableSize()
    {
        // Hand-written implementations are always treated as variable-size (there is no way to prove
        // otherwise without generating them ourselves), so - same as a variable-size [ByteSerializable]
        // type - they cannot be used as a dictionary key.
        string source = $$"""
            using System.Collections.Generic;
            using MessagingByteSerialization;

            namespace TestNs;

            {{ManualPointSource}}

            [ByteSerializable]
            public partial record BadMessage(Dictionary<ManualPoint, int> Counts);
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);

        Assert.Contains(result.GeneratorDiagnostics, d => d.Id == "MBS008");
        Assert.False(result.HasGeneratedSource("BadMessage"));
    }

    [Fact]
    public void OuterType_IsClassifiedAsVariableSizeEvenThoughMemberIsActuallyFixed()
    {
        // ManualPoint.GetByteSize() always returns a constant 8, but the generator has no way to know
        // that without generating it itself, so Message must NOT get a FixedByteSize const - it has to
        // fall back to a real GetByteSize() method that calls ManualPoint.GetByteSize() at runtime.
        string source = $$"""
            using MessagingByteSerialization;

            namespace TestNs;

            {{ManualPointSource}}

            [ByteSerializable]
            public partial record Message(ManualPoint Location);
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);

        string generated = result.GetGeneratedText("Message.ByteSerializable");
        Assert.DoesNotContain("FixedByteSize", generated);
    }
}
