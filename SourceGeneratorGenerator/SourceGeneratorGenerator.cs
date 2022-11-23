using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace SourceGeneratorGenerator;
[Generator]
public class SourceGeneratorGenerator : GeneratorBase<EverythingSyntaxReceiver>
{
    const string autogen = """
#nullable enable
using System;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
namespace SourceGeneratorGenerator {
    [AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false)]
    class SourceGeneratorAttribute : Attribute
    {

    }
    interface IGeneratorBase
    {
        void OnExecute(GeneratorExecutionContext context, IReadOnlyList<(SyntaxNode SyntaxNode, ISymbol? Symbol)> Nodes);
    }
}
""";
    protected override EverythingSyntaxReceiver ConstructSyntaxReceiver()
        => new();
    protected override void OnInitialize(GeneratorInitializationContext context)
    {
        context.RegisterForPostInitialization((i) => i.AddSource("SourceGeneratorFiles.cs", autogen));
    }
    protected override void OnExecute(GeneratorExecutionContext context, EverythingSyntaxReceiver SyntaxReceiver)
    {
        var Nodes = SyntaxReceiver.SyntaxNodes;
        foreach (var func in SyntaxReceiver.GeneratorClasses.AsParallel().Select(x => CompileClass(x.Syntax, x.Symbol)))
            if (func is not null)
                func(context, Nodes);
        context.AddSource("Error.SourceGeneratorGenerator.g.cs", $"/*\n{Error}\n*/");
    }
    static SourceGeneratorGenerator()
    {
        var dir = Path.GetDirectoryName(typeof(GeneratorSyntaxContext).Assembly.Location);
        foreach (var file in Directory.GetFiles(dir))
            if (file.EndsWith(".dll") && !file.Contains("Native") && !file.Contains("e_sqlite3.dll"))
                References.Add(MetadataReference.CreateFromFile(file));
    }
    static readonly List<MetadataReference> References = new()
    {
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(GeneratorSyntaxContext).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location.Replace("\\mscorlib.dll", "\\netstandard.dll")),
        MetadataReference.CreateFromFile(typeof(System.Collections.Immutable.ImmutableArray).Assembly.Location)
    };
    string Error = "";
    delegate void GeneratorCall(GeneratorExecutionContext context, IReadOnlyList<(SyntaxNode SyntaxNode, ISymbol? Symbol)> SyntaxReceiver);
    // Code copied an dmodified from https://github.com/hermanussen/CompileTimeMethodExecutionGenerator
    private GeneratorCall? CompileClass(ClassDeclarationSyntax @class, INamedTypeSymbol NamedType)
    {
        CSharpParseOptions options = @class.SyntaxTree.Options as CSharpParseOptions ?? throw new ArgumentException("method.SyntaxTree.Options is not CSharpParseOptions");

        CSharpCompilation compilation = CSharpCompilation.Create(
            Path.GetRandomFileName(),
            syntaxTrees: new[] {
                @class.SyntaxTree,
                CSharpSyntaxTree.ParseText(
                    SourceText.From(
                        autogen, Encoding.UTF8
                    ),
                    options
                )
            },
            references: References,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        using var ms = new MemoryStream();
        EmitResult result = compilation.Emit(ms);

        if (!result.Success)
        {
            IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                diagnostic.IsWarningAsError ||
                diagnostic.Severity == DiagnosticSeverity.Error);
            Error += $"Error From {NamedType}\n" + string.Join("\r\n", failures.Select(f => $"{f.Id} {f.GetMessage()}"));
            return null;
        }
        else
        {
            ms.Seek(0, SeekOrigin.Begin);
            Assembly assembly = Assembly.Load(ms.ToArray());

            Type type = assembly.GetType(NamedType.ToString());
            object obj = Activator.CreateInstance(type);
            return (a1, a2) =>
                type.InvokeMember("OnExecute",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                obj,
                new object[] { a1, a2 })?.ToString();
        }
    }
}
public class EverythingSyntaxReceiver : ISyntaxContextReceiver
{
    public readonly List<(SyntaxNode SyntaxNode, ISymbol? Symbol)> SyntaxNodes = new();
    public readonly List<(ClassDeclarationSyntax Syntax, INamedTypeSymbol Symbol)> GeneratorClasses = new();
    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        SyntaxNodes.Add((context.Node, context.SemanticModel.GetDeclaredSymbol(context.Node)));
        if (context.Node is ClassDeclarationSyntax classDeclarationSyntax)
        {
            var SourceGeneratorAttributeType =
                context.SemanticModel.Compilation
                .GetTypeByMetadataName("SourceGeneratorGenerator.SourceGeneratorAttribute");
            var IGeneratorBaseType =
                context.SemanticModel.Compilation
                .GetTypeByMetadataName("SourceGeneratorGenerator.IGeneratorBase");
            // Get the symbol being declared by the field, and keep it if its annotated;
            if (
                    context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax) is INamedTypeSymbol namedTypeSymbol &&
                    namedTypeSymbol.GetAttributes().SingleOrDefault(
                        x => x.AttributeClass?.Equals(SourceGeneratorAttributeType, SymbolEqualityComparer.Default) ?? false
                    ) is not null
                )
            {
                if (namedTypeSymbol.AllInterfaces.Contains(IGeneratorBaseType, SymbolEqualityComparer.Default))
                    GeneratorClasses.Add((classDeclarationSyntax, namedTypeSymbol));
            }
        }
    }
}