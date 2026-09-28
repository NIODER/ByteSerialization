using MessagingByteSerialization.Generators.Tests.Infrastructure;

namespace MessagingByteSerialization.Generators.Tests;

public class FactoryTests
{
    [Fact]
    public void MultipleTypes_DispatchThroughFactory()
    {
        const string source = """
            using System;
            using MessagingByteSerialization;
            using MessagingByteSerialization.Generated;

            namespace TestNs;

            [ByteSerializable]
            public partial record struct Ping(int Sequence);

            [ByteSerializable]
            public partial record Pong(int Sequence, string Note);

            public static class Harness
            {
                public static string Verify()
                {
                    var ping = new Ping(5);
                    byte[] pingBytes = ByteSerializableFactory.Serialize(ping);
                    if (pingBytes.Length != ByteSerializableFactory.GetEnvelopeSize(ping)) return "envelope size mismatch for Ping";

                    bool ok = MessagingByteSerialization.Generated.ByteSerializableFactory.TryDeserialize(pingBytes, out var message, out int read);
                    if (!ok) return "TryDeserialize failed for Ping";
                    if (read != pingBytes.Length) return "bytesRead mismatch for Ping";
                    if (message is not Ping restoredPing) return "wrong type for Ping";
                    if (restoredPing.Sequence != 5) return "Ping value mismatch";

                    var pong = new Pong(9, "hi");
                    byte[] pongBytes = ByteSerializableFactory.Serialize(pong);

                    ok = MessagingByteSerialization.Generated.ByteSerializableFactory.TryDeserialize(pongBytes, out message, out read);
                    if (!ok) return "TryDeserialize failed for Pong";
                    if (message is not Pong restoredPong) return "wrong type for Pong";
                    if (restoredPong.Sequence != 9 || restoredPong.Note != "hi") return "Pong value mismatch";

                    byte[] tooShort = new byte[] { 1 };
                    if (MessagingByteSerialization.Generated.ByteSerializableFactory.TryDeserialize(tooShort, out _, out _))
                    {
                        return "expected failure for too-short input";
                    }

                    byte[] bogus = new byte[] { 0xFF, 0xFF, 1, 2, 3 };
                    if (MessagingByteSerialization.Generated.ByteSerializableFactory.TryDeserialize(bogus, out _, out _))
                    {
                        return "expected failure for unknown type code";
                    }

                    if (Ping.TypeHash == Pong.TypeHash) return "Ping and Pong hashed to the same TypeHash";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.True(result.HasGeneratedSource("ByteSerializableFactory"));
        Assert.Equal("", result.RunHarness());
    }

    [Fact]
    public void NoByteSerializableTypes_NoFactoryGenerated()
    {
        const string source = """
            namespace TestNs;

            public class PlainClass
            {
                public int Id { get; set; }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);

        Assert.False(result.HasGeneratedSource("ByteSerializableFactory"));
    }

    record Seri() : IByteSerializable<Seri>
    {
        public static ushort TypeHash => throw new NotImplementedException();

        public static Seri FromBytes(ReadOnlySpan<byte> source, out int bytesRead)
        {
            throw new NotImplementedException();
        }

        public int GetByteSize()
        {
            throw new NotImplementedException();
        }

        public void ToBytes(Span<byte> destination)
        {
            throw new NotImplementedException();
        }
    }
}
