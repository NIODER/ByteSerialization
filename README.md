# Common
Library for auto-implementing byte serialization/deserialization. Implements 

Generates two methods with implementation:

```
void ToBytes(Span<byte> destination);
static TSelf FromBytes(ReadOnlySpan<byte> source, out int bytesRead);
```

## Usage

For generation serialization methods use `[ByteSerializable]` and partial modifier for selected DTO.

add in project interface implementation and generator.

```
  <ItemGroup>
    <ProjectReference Include="..\MessagingByteSerialization.Abstractions\MessagingByteSerialization.Abstractions.csproj" />
    <ProjectReference Include="..\MessagingByteSerialization.Generators\MessagingByteSerialization.Generators.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
  </ItemGroup>
```
