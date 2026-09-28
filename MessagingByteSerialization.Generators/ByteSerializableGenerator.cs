using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MessagingByteSerialization.Generators.Analysis;
using MessagingByteSerialization.Generators.Emit;
using MessagingByteSerialization.Generators.Model;

namespace MessagingByteSerialization.Generators;

/// <summary>
/// Generates <c>ToBytes</c>/<c>FromBytes</c>/<c>GetByteSize</c>/<c>TypeHash</c> for every type marked
/// <c>[ByteSerializable]</c>, plus a single aggregating <c>ByteSerializableFactory</c> for the compilation.
/// See <see cref="Analysis.TypeAnalyzer"/> for the classification rules and <see cref="Emit.SourceEmitter"/>
/// for the wire format.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class ByteSerializableGenerator : IIncrementalGenerator
{
    private const string AttributeMetadataName = "MessagingByteSerialization.ByteSerializableAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<AnalyzedType> analyzed = context.SyntaxProvider.ForAttributeWithMetadataName(
            AttributeMetadataName,
            predicate: static (node, _) => node is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax,
            transform: static (ctx, ct) =>
            {
                var diagnostics = new List<Diagnostic>();
                TypeModel? model = TypeAnalyzer.Analyze(ctx, diagnostics, ct);
                return new AnalyzedType(model, [.. diagnostics]);
            });

        context.RegisterSourceOutput(analyzed, static (spc, analyzedType) =>
        {
            foreach (Diagnostic diagnostic in analyzedType.Diagnostics)
            {
                spc.ReportDiagnostic(diagnostic);
            }

            if (analyzedType.Model is { } model)
            {
                spc.AddSource(HintName(model), SourceEmitter.Emit(model));
            }
        });

        IncrementalValueProvider<ImmutableArray<TypeModel>> collectedModels = analyzed
            .Select(static (a, _) => a.Model)
            .Where(static m => m is not null)
            .Select(static (m, _) => m!)
            .Collect();

        context.RegisterSourceOutput(collectedModels, static (spc, models) =>
        {
            var diagnostics = new List<Diagnostic>();
            string? factorySource = FactoryEmitter.Emit(models, diagnostics);

            foreach (Diagnostic diagnostic in diagnostics)
            {
                spc.ReportDiagnostic(diagnostic);
            }

            if (factorySource is not null)
            {
                spc.AddSource($"{FactoryEmitter.FactoryClassName}.g.cs", factorySource);
            }
        });
    }

    private static string HintName(TypeModel model)
    {
        string qualified = model.Namespace.Length == 0 ? model.Name : $"{model.Namespace}.{model.Name}";
        return $"{qualified.Replace('.', '_')}.ByteSerializable.g.cs";
    }

    private readonly struct AnalyzedType(TypeModel? model, ImmutableArray<Diagnostic> diagnostics)
    {
        public TypeModel? Model { get; } = model;

        public ImmutableArray<Diagnostic> Diagnostics { get; } = diagnostics;
    }
}
