using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SharpGen.Logging;
using SharpGen.Model;
using SharpGen.Transform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SharpGen.Generator
{
    class RoslynGenerator
    {
        private readonly GlobalNamespaceProvider globalNamespace;

        public IGeneratorRegistry Generators { get; }

        public RoslynGenerator(Logger logger, GlobalNamespaceProvider globalNamespace, IDocumentationAggregator documentation)
        {
            Logger = logger;
            this.globalNamespace = globalNamespace;
            Generators = new DefaultGenerators(globalNamespace, documentation);
        }

        public Logger Logger { get; }

        public void Run(string generatedCodeFolder, IEnumerable<CsAssembly> assemblies)
        {
            var trees = new List<SyntaxTree>();

            // Iterates on assemblies
            foreach (var csAssembly in assemblies.Where(assembly => assembly.IsToUpdate))
            {
                var generatedDirectoryForAssembly = Path.Combine(csAssembly.RootDirectory, generatedCodeFolder ?? "Generated");
                
                var directoryToCreate = new HashSet<string>(StringComparer.CurrentCulture);
                
                // Remove the generated directory before creating it
                if (!directoryToCreate.Contains(generatedDirectoryForAssembly))
                {
                    directoryToCreate.Add(generatedDirectoryForAssembly);
                    if (Directory.Exists(generatedDirectoryForAssembly))
                    {
                        foreach (var oldGeneratedFile in Directory.EnumerateFiles(generatedDirectoryForAssembly, "*.cs", SearchOption.AllDirectories))
                        {
                            try
                            {
                                File.Delete(oldGeneratedFile);
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }

                if (!Directory.Exists(generatedDirectoryForAssembly))
                    Directory.CreateDirectory(generatedDirectoryForAssembly);

                Logger.Message("Process Assembly {0} => {1}", csAssembly.Name, generatedDirectoryForAssembly);

                // LocalInterop is once generated per assembly
                trees.Add(
                    CSharpSyntaxTree.Create(
                        CompilationUnit().WithMembers(
                            SingletonList<MemberDeclarationSyntax>(
                                Generators.LocalInterop.GenerateCode(csAssembly))
                            )
                            .NormalizeWhitespace(elasticTrivia: true))
                        .WithFilePath(Path.Combine(generatedDirectoryForAssembly, "LocalInterop.cs")));
                
                foreach (var csNamespace in csAssembly.Namespaces)
                {
                    var subDirectory = csNamespace.OutputDirectory ?? ".";

                    var nameSpaceDirectory = Path.Combine(generatedDirectoryForAssembly, subDirectory);
                    if (!Directory.Exists(nameSpaceDirectory))
                        Directory.CreateDirectory(nameSpaceDirectory);

                    trees.Add(
                        CSharpSyntaxTree.Create(
                            GenerateCompilationUnit(csNamespace.Name, csNamespace.Enums, Generators.Enum))
                            .WithFilePath(Path.Combine(nameSpaceDirectory, "Enumerations.cs")));
                    trees.Add(
                        CSharpSyntaxTree.Create(
                            GenerateCompilationUnit(csNamespace.Name, csNamespace.Structs, Generators.Struct))
                            .WithFilePath(Path.Combine(nameSpaceDirectory, "Structures.cs")));
                    trees.Add(
                        CSharpSyntaxTree.Create(
                            GenerateCompilationUnit(csNamespace.Name, csNamespace.Classes, Generators.Group))
                            .WithFilePath(Path.Combine(nameSpaceDirectory, "Functions.cs")));
                    trees.Add(
                        CSharpSyntaxTree.Create(
                            GenerateCompilationUnit(csNamespace.Name, csNamespace.Interfaces, Generators.Interface))
                            .WithFilePath(Path.Combine(nameSpaceDirectory, "Interfaces.cs")));
                }
            }

            foreach (var tree in trees)
            {
                File.WriteAllText(tree.FilePath, tree.GetCompilationUnitRoot().ToFullString());
            }
        }

        private static CompilationUnitSyntax GenerateCompilationUnit<T>(
            string csNamespace,
            IEnumerable<T> elements,
            IMultiCodeGenerator<T, MemberDeclarationSyntax> generator)
        {
            return CompilationUnit()
                .WithMembers(
                    SingletonList<MemberDeclarationSyntax>(
                        NamespaceDeclaration(ParseName(csNamespace))
                            .WithMembers(List(elements.SelectMany(element => generator.GenerateCode(element))))
                    ))
                .NormalizeWhitespace(elasticTrivia: true);
        }
    }
}
