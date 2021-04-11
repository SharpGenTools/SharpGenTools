using System;
using SharpGen.Platform.Clang.Abstractions;
using SharpGen.Platform.Clang.CSharp;

namespace SharpGen.Platform
{
    internal sealed class SharpGenOutputBuilder : IOutputBuilder
    {
        private readonly CSharpOutputBuilder _outputBuilderImplementation;

        public SharpGenOutputBuilder(CSharpOutputBuilder outputBuilderImplementation)
        {
            _outputBuilderImplementation = outputBuilderImplementation ??
                                           throw new ArgumentNullException(nameof(outputBuilderImplementation));
        }

        public void BeginInnerValue()
        {
            _outputBuilderImplementation.BeginInnerValue();
        }

        public void EndInnerValue()
        {
            _outputBuilderImplementation.EndInnerValue();
        }

        public void BeginInnerCast()
        {
            _outputBuilderImplementation.BeginInnerCast();
        }

        public void WriteCastType(string targetTypeName)
        {
            _outputBuilderImplementation.WriteCastType(targetTypeName);
        }

        public void EndInnerCast()
        {
            _outputBuilderImplementation.EndInnerCast();
        }

        public void BeginUnchecked()
        {
            _outputBuilderImplementation.BeginUnchecked();
        }

        public void EndUnchecked()
        {
            _outputBuilderImplementation.EndUnchecked();
        }

        public void BeginConstant(in ConstantDesc desc)
        {
            _outputBuilderImplementation.BeginConstant(in desc);
        }

        public void BeginConstantValue(bool isGetOnlyProperty = false)
        {
            _outputBuilderImplementation.BeginConstantValue(isGetOnlyProperty);
        }

        public void WriteConstantValue(long value)
        {
            _outputBuilderImplementation.WriteConstantValue(value);
        }

        public void WriteConstantValue(ulong value)
        {
            _outputBuilderImplementation.WriteConstantValue(value);
        }

        public void EndConstantValue()
        {
            _outputBuilderImplementation.EndConstantValue();
        }

        public void EndConstant(bool isConstant)
        {
            _outputBuilderImplementation.EndConstant(isConstant);
        }

        public void BeginEnum(in EnumDesc desc)
        {
            _outputBuilderImplementation.BeginEnum(in desc);
        }

        public void EndEnum()
        {
            _outputBuilderImplementation.EndEnum();
        }

        public void BeginField(in FieldDesc desc)
        {
            _outputBuilderImplementation.BeginField(in desc);
        }

        public void WriteFixedCountField(string typeName, string escapedName, string fixedName, string count)
        {
            _outputBuilderImplementation.WriteFixedCountField(typeName, escapedName, fixedName, count);
        }

        public void WriteRegularField(string typeName, string escapedName)
        {
            _outputBuilderImplementation.WriteRegularField(typeName, escapedName);
        }

        public void EndField(bool isBodyless = true)
        {
            _outputBuilderImplementation.EndField(isBodyless);
        }

        public void BeginFunctionOrDelegate<TCustomAttrGeneratorData>(in FunctionOrDelegateDesc<TCustomAttrGeneratorData> info, ref bool isMethodClassUnsafe)
        {
            if (info.NeedsReturnFixup)
                _outputBuilderImplementation.WriteIndentedLine("[NeedsReturnFixup]");

            _outputBuilderImplementation.BeginFunctionOrDelegate(in info, ref isMethodClassUnsafe);
        }

        public void BeginFunctionInnerPrototype(string escapedName)
        {
            _outputBuilderImplementation.BeginFunctionInnerPrototype(escapedName);
        }

        public void BeginParameter<TCustomAttrGeneratorData>(in ParameterDesc<TCustomAttrGeneratorData> info)
        {
            _outputBuilderImplementation.BeginParameter(in info);
        }

        public void BeginParameterDefault()
        {
            _outputBuilderImplementation.BeginParameterDefault();
        }

        public void EndParameterDefault()
        {
            _outputBuilderImplementation.EndParameterDefault();
        }

        public void EndParameter()
        {
            _outputBuilderImplementation.EndParameter();
        }

        public void WriteParameterSeparator()
        {
            _outputBuilderImplementation.WriteParameterSeparator();
        }

        public void EndFunctionInnerPrototype()
        {
            _outputBuilderImplementation.EndFunctionInnerPrototype();
        }

        public void BeginConstructorInitializer(string memberRefName, string memberInitName)
        {
            _outputBuilderImplementation.BeginConstructorInitializer(memberRefName, memberInitName);
        }

        public void EndConstructorInitializer()
        {
            _outputBuilderImplementation.EndConstructorInitializer();
        }

        public void BeginBody(bool isExpressionBody = false)
        {
            _outputBuilderImplementation.BeginBody(isExpressionBody);
        }

        public void BeginConstructorInitializers()
        {
            _outputBuilderImplementation.BeginConstructorInitializers();
        }

        public void EndConstructorInitializers()
        {
            _outputBuilderImplementation.EndConstructorInitializers();
        }

        public void BeginInnerFunctionBody()
        {
            _outputBuilderImplementation.BeginInnerFunctionBody();
        }

        public void EndInnerFunctionBody()
        {
            _outputBuilderImplementation.EndInnerFunctionBody();
        }

        public void EndBody(bool isExpressionBody = false)
        {
            _outputBuilderImplementation.EndBody(isExpressionBody);
        }

        public void EndFunctionOrDelegate(bool isVirtual, bool isBodyless)
        {
            _outputBuilderImplementation.EndFunctionOrDelegate(isVirtual, isBodyless);
        }

        public void BeginStruct<TCustomAttrGeneratorData>(in StructDesc<TCustomAttrGeneratorData> info)
        {
            var layoutDesc = info.Layout;

            if (info.HasVtbl)
                _outputBuilderImplementation.WriteIndentedLine("[HasVtbl]");

            if (info.IsUnion)
                _outputBuilderImplementation.WriteIndentedLine("[NativeTypeUnion]");

            _outputBuilderImplementation.WriteIndented("[NativeTypeLayout(");
            _outputBuilderImplementation.Write($"{nameof(layoutDesc.Alignment32)} = {layoutDesc.Alignment32}, ");
            _outputBuilderImplementation.Write($"{nameof(layoutDesc.Alignment64)} = {layoutDesc.Alignment64}, ");
            _outputBuilderImplementation.Write($"{nameof(layoutDesc.Size32)} = {layoutDesc.Size32}, ");
            _outputBuilderImplementation.Write($"{nameof(layoutDesc.Size64)} = {layoutDesc.Size64}, ");
            _outputBuilderImplementation.Write($"{nameof(layoutDesc.Pack)} = {layoutDesc.Pack}");
            _outputBuilderImplementation.WriteLine(")]");

            _outputBuilderImplementation.BeginStruct(in info);
        }

        public void BeginExplicitVtbl()
        {
            _outputBuilderImplementation.BeginExplicitVtbl();
        }

        public void EndExplicitVtbl()
        {
            _outputBuilderImplementation.EndExplicitVtbl();
        }

        public void EndStruct()
        {
            _outputBuilderImplementation.EndStruct();
        }

        public void EmitCompatibleCodeSupport()
        {
            _outputBuilderImplementation.EmitCompatibleCodeSupport();
        }

        public void EmitFnPtrSupport()
        {
            _outputBuilderImplementation.EmitFnPtrSupport();
        }

        public void EmitSystemSupport()
        {
            _outputBuilderImplementation.EmitSystemSupport();
        }

        public CSharpOutputBuilder BeginCSharpCode()
        {
            return _outputBuilderImplementation.BeginCSharpCode();
        }

        public void EndCSharpCode(CSharpOutputBuilder output)
        {
            _outputBuilderImplementation.EndCSharpCode(output);
        }

        public void BeginGetter(bool aggressivelyInlined)
        {
            _outputBuilderImplementation.BeginGetter(aggressivelyInlined);
        }

        public void EndGetter()
        {
            _outputBuilderImplementation.EndGetter();
        }

        public void BeginSetter(bool aggressivelyInlined)
        {
            _outputBuilderImplementation.BeginSetter(aggressivelyInlined);
        }

        public void EndSetter()
        {
            _outputBuilderImplementation.EndSetter();
        }

        public void BeginIndexer(AccessSpecifier accessSpecifier, bool isUnsafe)
        {
            _outputBuilderImplementation.BeginIndexer(accessSpecifier, isUnsafe);
        }

        public void WriteIndexer(string typeName)
        {
            _outputBuilderImplementation.WriteIndexer(typeName);
        }

        public void BeginIndexerParameters()
        {
            _outputBuilderImplementation.BeginIndexerParameters();
        }

        public void EndIndexerParameters()
        {
            _outputBuilderImplementation.EndIndexerParameters();
        }

        public void EndIndexer()
        {
            _outputBuilderImplementation.EndIndexer();
        }

        public void BeginDereference()
        {
            _outputBuilderImplementation.BeginDereference();
        }

        public void EndDereference()
        {
            _outputBuilderImplementation.EndDereference();
        }

        public void WriteDivider(bool force = false)
        {
            _outputBuilderImplementation.WriteDivider(force);
        }

        public void SuppressDivider()
        {
            _outputBuilderImplementation.SuppressDivider();
        }

        public void WriteCustomAttribute(string attribute)
        {
            _outputBuilderImplementation.WriteCustomAttribute(attribute);
        }

        public void WriteIid(string iidName, string iidValue)
        {
            _outputBuilderImplementation.WriteIid(iidName, iidValue);
        }

        public void EmitUsingDirective(string directive)
        {
            _outputBuilderImplementation.EmitUsingDirective(directive);
        }

        public bool IsTestOutput => _outputBuilderImplementation.IsTestOutput;

        public string Name => _outputBuilderImplementation.Name;

        public string Extension => _outputBuilderImplementation.Extension;
    }
}
