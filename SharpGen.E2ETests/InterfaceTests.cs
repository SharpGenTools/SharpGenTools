using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SharpGen.E2ETests
{
    public class InterfaceTests : TestBase
    {
        private const string ComIncludeProlog = @"
    // Use unicode
    #define UNICODE
    
    // for SAL annotations
    #define _PREFAST_
    
    // To force GUID to be declared
    #define INITGUID
    
    #define _ALLOW_KEYWORD_MACROS
    
    // Wrap all declspec for code-gen
    #define __declspec(x) __attribute__((annotate(#x)))";
        
        private static void AssertMethodCalliIndex(Microsoft.CodeAnalysis.ISymbol interfaceMethod, int correctIndex)
        {
            var methodSyntax = interfaceMethod.DeclaringSyntaxReferences[0].GetSyntax();
            var calliCall = methodSyntax.DescendantNodes().OfType<InvocationExpressionSyntax>().First();
            var nativePtrArgument = calliCall.ArgumentList.Arguments.First().Expression;
            Assert.IsType<IdentifierNameSyntax>(nativePtrArgument);
            var functionPtrArgument = calliCall.ArgumentList.Arguments.Last().Expression;
            var methodIndex = functionPtrArgument.DescendantNodes().OfType<BracketedArgumentListSyntax>().First();
            Assert.Equal(correctIndex.ToString(), methodIndex.Arguments.Single().ToString());
        }

        [Fact]
        public void BasicComInterfaceGeneratesCorrectClass()
        {
            var testDirectory = GenerateTestDirectory();
            var config = new Config.ConfigFile
            {
                Namespace = nameof(BasicComInterfaceGeneratesCorrectClass),
                Assembly = nameof(BasicComInterfaceGeneratesCorrectClass),
                IncludeProlog =
                {
                    ComIncludeProlog
                },
                IncludeDirs =
                {
                    new Config.IncludeDirRule(@"=HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows Kits\Installed Roots\KitsRoot10;Include\10.0.15063.0\shared"),
                    new Config.IncludeDirRule(@"=HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows Kits\Installed Roots\KitsRoot10;Include\10.0.15063.0\um"),
                    new Config.IncludeDirRule(@"=HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows Kits\Installed Roots\KitsRoot10;Include\10.0.15063.0\ucrt"),
                    new Config.IncludeDirRule(@"$(VC_TOOLS_PATH)\include")
                },
                Includes =
                {
                    new Config.IncludeRule
                    {
                        File = "windows.h"
                    },
                    new Config.IncludeRule
                    {
                        File= "Wbemcli.h",
                        AttachTypes = new List<string>
                        {
                            "IUnsecuredApartment"
                        },
                        Namespace = nameof(BasicComInterfaceGeneratesCorrectClass)
                    }
                },
                Bindings =
                {
                    new Config.BindRule("int", "System.Int32"),
                    new Config.BindRule("IUnknown", "SharpDX.ComObject")
                },
            };

            (int exitCode, string output) = RunWithConfig(testDirectory, config);
            AssertRanSuccessfully(exitCode, output);
            var compilation = GetCompilationForGeneratedCode(testDirectory);
            var iUnsecuredApartment = compilation.GetTypeByMetadataName($"{nameof(BasicComInterfaceGeneratesCorrectClass)}.IUnsecuredApartment");
            var interfaceMethod = iUnsecuredApartment.GetMembers("CreateObjectStub").Single();
            AssertMethodCalliIndex(interfaceMethod, 3);
        }

        [Fact]
        public void IUnknownMappingGeneratesCorrectClass()
        {
            var testDirectory = GenerateTestDirectory();
            var config = new Config.ConfigFile
            {
                Namespace = nameof(IUnknownMappingGeneratesCorrectClass),
                Assembly = nameof(IUnknownMappingGeneratesCorrectClass),
                IncludeProlog =
                {
                    ComIncludeProlog
                },
                IncludeDirs =
                {
                    new Config.IncludeDirRule(@"=HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows Kits\Installed Roots\KitsRoot10;Include\10.0.15063.0\shared"),
                    new Config.IncludeDirRule(@"=HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows Kits\Installed Roots\KitsRoot10;Include\10.0.15063.0\um"),
                    new Config.IncludeDirRule(@"=HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows Kits\Installed Roots\KitsRoot10;Include\10.0.15063.0\ucrt"),
                    new Config.IncludeDirRule(@"$(VC_TOOLS_PATH)\include")
                },
                Includes =
                {
                    new Config.IncludeRule
                    {
                        File = "windows.h"
                    },
                    new Config.IncludeRule
                    {
                        File = "Unknwnbase.h",
                        AttachTypes = new List<string>
                        {
                            "IUnknown",
                        },
                        Namespace = nameof(IUnknownMappingGeneratesCorrectClass)
                    }
                },
                Bindings =
                {
                    new Config.BindRule("int", "System.Int32"),
                    new Config.BindRule("GUID", "System.Guid"),
                    new Config.BindRule("void", "System.Void"),
                    new Config.BindRule("unsigned int", "System.UInt32")
                }
            };

            (int exitCode, string output) = RunWithConfig(testDirectory, config);
            AssertRanSuccessfully(exitCode, output);
            var compilation = GetCompilationForGeneratedCode(testDirectory);
            var iUnknown = compilation.GetTypeByMetadataName($"{nameof(IUnknownMappingGeneratesCorrectClass)}.IUnknown");
            AssertMethodCalliIndex(iUnknown.GetMembers("QueryInterface").Single(), 0);
            AssertMethodCalliIndex(iUnknown.GetMembers("AddRef").Single(), 1);
            AssertMethodCalliIndex(iUnknown.GetMembers("Release").Single(), 2);
        }
    }
}
