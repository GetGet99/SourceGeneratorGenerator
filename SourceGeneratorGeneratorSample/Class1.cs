using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGeneratorGenerator;

namespace SourceGeneratorGeneratorSample;

[SourceGenerator]
public class CopySourceCode : IGeneratorBase
{
    public void OnExecute(GeneratorExecutionContext context, IReadOnlyList<(SyntaxNode SyntaxNode, ISymbol? Symbol)> Nodes)
    {
        try
        {
            var CopySourceFromAtributeType = context.Compilation.GetTypeByMetadataName(typeof(CopySourceAttribute).FullName);
            // context.Compilation
            foreach (var (SyntaxNode, Symbol) in Nodes)
            {
                if (Symbol is INamedTypeSymbol ClassSymbol)
                {
                    if (!ClassSymbol.IsType) return;
                    var attributes = (
                        from attr in ClassSymbol.GetAttributes()
                        where attr.AttributeClass.Equals(CopySourceFromAtributeType, SymbolEqualityComparer.Default)
                        where attr.ConstructorArguments.Length == 2
                        let parsed = (
                            MemberName: attr.ConstructorArguments[0].Value?.ToString(),
                            Type: attr.ConstructorArguments[1].Value as INamedTypeSymbol
                        )
                        where parsed.MemberName is not null && parsed.Type is not null
                        select parsed
                    ).ToArray();
                    if (attributes.Length > 0)
                    {
                        context.AddSource($"{ClassSymbol}.CopySourceGenerated.g.cs", $$"""
                        namespace {{ClassSymbol.ContainingNamespace}} {
                            partial {{ClassSymbol.TypeKind.ToString().ToLower()}} {{ClassSymbol.Name}} {
                                {{
                                    string.Join("\n\n",
                                        from attribute in attributes
                                        select $"const string {attribute.MemberName} = \"\"\"\n" +
                                        $"{attribute.Type.DeclaringSyntaxReferences[0].SyntaxTree}\n\"\"\";".Indent(3)
                                    )
                                }}
                            }
                        }
                        """);
                    }
                }
            }
        } catch (Exception e)
        {
            context.AddSource("Exception.cs", $"/* {e.GetType()} {e.Message} {e.StackTrace} */");
        }
    }
}
[AttributeUsage(AttributeTargets.Class)]
public class CopySourceAttribute : Attribute
{
    public CopySourceAttribute(string MemberName, Type Type)
    {

    }
}
static partial class Extension
{
    public static string Indent(this string Original, int IndentTimes = 1, int IndentSpace = 4)
    {
        var Indent = new string(' ', IndentSpace * IndentTimes);
        var slashNindent = $"\n{Indent}";
        return Indent + Original.Replace("\n", slashNindent);
    }
}