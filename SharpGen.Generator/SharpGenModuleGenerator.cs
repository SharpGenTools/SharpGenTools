using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator;

[Generator]
public sealed partial class SharpGenModuleGenerator : ISourceGenerator
{
    private const string CallbackableInterfaceName = "SharpGen.Runtime.ICallbackable";
    private const string CallbackBaseClassName = "SharpGen.Runtime.CallbackBase";
    private const string ModuleInitializerAttributeName = "System.Runtime.CompilerServices.ModuleInitializerAttribute";

    private static readonly AttributeListSyntax[] ModuleInitializerAttributeList = new[]
    {
        // Module Initializer.
        AttributeList(
            SingletonSeparatedList(Attribute(ParseName(ModuleInitializerAttributeName)))
        )
    };

    private static readonly GlobalStatementSyntax SuppressWarningsStatement = GlobalStatement
    (
        ExpressionStatement(IdentifierName(
            Identifier(
                TriviaList
                (
                    Trivia(IfDirectiveTrivia(IdentifierName("NET6_0_OR_GREATER"), true, false, false)),
                    DisabledText(@"[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(""ReflectionAnalysis"", ""IL2111"")]
[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(""ReflectionAnalysis"", ""IL2110"")]
"),
                    Trivia(EndIfDirectiveTrivia(true))
                ),
            "", TriviaList()))).WithSemicolonToken(MissingToken(SyntaxKind.SemicolonToken))
    );

    private static readonly NameSyntax TypeDataStorage = ParseName("SharpGen.Runtime.TypeDataStorage");

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(static () => new SemanticTreeWalker());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var configOptions = context.AnalyzerConfigOptions.GlobalOptions;
        var waitForDebuggerAttach = false;

#pragma warning disable CA1806
        if (configOptions.TryGetValue("build_property.SharpGenWaitForRoslynDebuggerAttach", out var value))
            bool.TryParse(value, out waitForDebuggerAttach);
#pragma warning restore CA1806

        if (waitForDebuggerAttach)
            while (!Debugger.IsAttached && !context.CancellationToken.IsCancellationRequested)
                Thread.Sleep(TimeSpan.FromSeconds(1));

        if (context.CancellationToken.IsCancellationRequested)
            return;

        if (context.CancellationToken.IsCancellationRequested)
            return;

        GenerateModule(context);
    }

    private static CompilationUnitSyntax GenerateCompilationUnit(
        params MemberDeclarationSyntax[] namespaceDeclarations
    ) => GenerateCompilationUnit((IEnumerable<MemberDeclarationSyntax>) namespaceDeclarations);

    private static CompilationUnitSyntax GenerateCompilationUnit(
        IEnumerable<MemberDeclarationSyntax> namespaceDeclarations
    ) => CompilationUnit(default, default, default, List(namespaceDeclarations))
       .NormalizeWhitespace(elasticTrivia: true);

    private sealed class SemanticTreeWalker : ISyntaxContextReceiver
    {
        public readonly HashSet<ITypeSymbol> QueuedJobs = new(SymbolEqualityComparer.Default);

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            var syntaxNode = context.Node;

            if (syntaxNode.Language != LanguageNames.CSharp)
                return;

            if (syntaxNode is InterfaceDeclarationSyntax or ClassDeclarationSyntax)
            {
                switch (context.SemanticModel.GetDeclaredSymbol(syntaxNode))
                {
                    case ITypeSymbol symbol:
                        QueuedJobs.Add(symbol);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}