using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SourceGeneratorGenerator;

public abstract class GeneratorBase<T> : ISourceGenerator where T : ISyntaxContextReceiver
{
    protected abstract T ConstructSyntaxReceiver();
    public void Initialize(GeneratorInitializationContext context)
    {
        OnInitialize(context);
        context.RegisterForSyntaxNotifications(() => ConstructSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is T receiver)
            OnExecute(context, receiver);
    }
    protected virtual void OnInitialize(GeneratorInitializationContext context) { }
    protected virtual void OnExecute(GeneratorExecutionContext context, T SyntaxReceiver) { }
}
