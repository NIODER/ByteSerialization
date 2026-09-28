using MessagingByteSerialization.Generators.Tests.Infrastructure;

namespace MessagingByteSerialization.Generators.Tests;

public class ByteSerializableIgnoreTests
{
    [Fact]
    public void IgnoredMember_IsNotWrittenAndComesBackAsDefault_PositionalRecord()
    {
        const string source = """
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial record Message(int Id, [property: ByteSerializableIgnore] int NotSerialized, string Name);

            public static class Harness
            {
                public static string Verify()
                {
                    var original = new Message(1, 999, "hello");
                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != original.GetByteSize()) return "size mismatch";

                    // NotSerialized must not contribute any bytes: size should match a message with no ignored field at all.
                    int expectedSize = sizeof(int) + (1 + 4 + 5); // Id (int) + string("hello": 1-byte kind + 4-byte len + 5 bytes)
                    if (bytes.Length != expectedSize) return $"unexpected size {bytes.Length}, expected {expectedSize}";

                    var restored = Message.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (restored.Id != 1) return "Id mismatch";
                    if (restored.Name != "hello") return "Name mismatch";
                    if (restored.NotSerialized != 0) return "NotSerialized should be default(int) == 0";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }

    [Fact]
    public void IgnoredMember_IsNotWrittenAndComesBackAsDefault_ParameterlessClass()
    {
        const string source = """
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial class MessageClass
            {
                public int Id { get; set; }

                [ByteSerializableIgnore]
                public string NotSerialized { get; set; } = "unset";
            }

            public static class Harness
            {
                public static string Verify()
                {
                    var original = new MessageClass { Id = 42, NotSerialized = "should not appear on the wire" };
                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != sizeof(int)) return $"expected {sizeof(int)} bytes, got {bytes.Length}";

                    var restored = MessageClass.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (restored.Id != 42) return "Id mismatch";
                    if (restored.NotSerialized is not null) return "NotSerialized should be default(string) == null";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }

    [Fact]
    public void IgnoredMemberOfUnsupportedType_StillCompiles()
    {
        // The whole point: an ignored member's type is never inspected, so it can be anything - even a
        // type the generator could never otherwise support.
        const string source = """
            using System;
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial record Message(int Id, [property: ByteSerializableIgnore] Action? Callback);

            public static class Harness
            {
                public static string Verify()
                {
                    bool called = false;
                    var original = new Message(1, () => called = true);
                    byte[] bytes = original.ToBytes();

                    var restored = Message.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (restored.Id != 1) return "Id mismatch";
                    if (restored.Callback is not null) return "Callback should be default(Action) == null";
                    if (called) return "original delegate should not have been touched";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }

    [Fact]
    public void AllMembersIgnored_ProducesEmptyPayload()
    {
        const string source = """
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial record Message([property: ByteSerializableIgnore] int A, [property: ByteSerializableIgnore] string B);

            public static class Harness
            {
                public static string Verify()
                {
                    var original = new Message(7, "ignored");
                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != 0) return $"expected empty payload, got {bytes.Length} bytes";
                    if (original.GetByteSize() != 0) return "expected GetByteSize() == 0";

                    var restored = Message.FromBytes(bytes, out int read);
                    if (read != 0) return "bytesRead should be 0";
                    if (restored.A != 0) return "A should be default(int) == 0";
                    if (restored.B is not null) return "B should be default(string) == null";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }

    [Fact]
    public void FixedSizeClassification_IsUnaffectedByIgnoredMember()
    {
        // A record with only fixed-size (non-ignored) members plus one ignored member of any type must
        // still get the FixedByteSize fast path - the ignored member contributes zero bytes either way.
        const string source = """
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial record struct Message(int X, int Y, [property: ByteSerializableIgnore] string Ignored);

            public static class Harness
            {
                public static string Verify()
                {
                    if (Message.FixedByteSize != sizeof(int) * 2) return $"unexpected FixedByteSize {Message.FixedByteSize}";

                    var original = new Message(1, 2, "unset");
                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != Message.FixedByteSize) return "size mismatch";

                    var restored = Message.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (restored.X != 1 || restored.Y != 2) return "value mismatch";
                    if (restored.Ignored is not null) return "Ignored should be default(string) == null";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }

    [Fact]
    public void IgnoredMember_MixedWithVariableSizeMembers_RoundTrips()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial record Message(List<string> Tags, [property: ByteSerializableIgnore] int Cache, string Name);

            public static class Harness
            {
                public static string Verify()
                {
                    var original = new Message(new List<string> { "a", "b" }, 12345, "hello");
                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != original.GetByteSize()) return "size mismatch";

                    var restored = Message.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (!restored.Tags.SequenceEqual(original.Tags)) return "Tags mismatch";
                    if (restored.Name != "hello") return "Name mismatch";
                    if (restored.Cache != 0) return "Cache should be default(int) == 0";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }
}
