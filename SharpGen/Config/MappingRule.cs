using System.Xml.Serialization;

namespace SharpGen.Config
{
    [XmlType("map")]
    public class MappingRule : MappingBaseRule
    {
        public MappingRule()
        {
            IsFinalMappingName = false;
            MethodCheckReturnType = true;
        }

        /// <summary>
        ///     Default Value for parameters
        /// </summary>
        [XmlAttribute("assembly")]
        public string Assembly { get; set; }

        /// <summary>
        ///     Default Value for parameters
        /// </summary>
        [XmlAttribute("namespace")]
        public string Namespace { get; set; }

        /// <summary>
        ///     Default Value for parameters
        /// </summary>
        [XmlAttribute("default")]
        public string DefaultValue { get; set; }

        [XmlIgnore] public bool? MethodCheckReturnType { get; set; }

        [XmlAttribute("check")]
        public bool _MethodCheckReturnType_
        {
            get => MethodCheckReturnType.Value;
            set => MethodCheckReturnType = value;
        }

        [XmlIgnore] public bool? AlwaysReturnHResult { get; set; }

        [XmlAttribute("hresult")]
        public bool _AlwaysReturnHResult_
        {
            get => AlwaysReturnHResult.Value;
            set => AlwaysReturnHResult = value;
        }

        /// <summary>
        ///     General visibility for Methods
        /// </summary>
        [XmlIgnore]
        public Visibility? Visibility { get; set; }

        [XmlAttribute("visibility")]
        public Visibility _Visibility_
        {
            get => Visibility.Value;
            set => Visibility = value;
        }

        /// <summary>
        ///     General visibility for DefaultCallback class
        /// </summary>
        [XmlIgnore]
        public Visibility? NativeCallbackVisibility { get; set; }

        [XmlAttribute("callback-visibility")]
        public Visibility _NativeCallbackVisibility_
        {
            get => NativeCallbackVisibility.Value;
            set => NativeCallbackVisibility = value;
        }

        [XmlIgnore] public NamingFlags? NamingFlags { get; set; }

        [XmlAttribute("naming")]
        public NamingFlags _NamingFlags_
        {
            get => NamingFlags.Value;
            set => NamingFlags = value;
        }

        /// <summary>
        ///     Name of a native callback
        /// </summary>
        [XmlAttribute("callback-name")]
        public string NativeCallbackName { get; set; }

        /// <summary>
        ///     Used for methods, to force a method to not be translated to a property
        /// </summary>
        [XmlIgnore]
        public bool? Property { get; set; }

        [XmlAttribute("property")]
        public bool _Property_
        {
            get => Property.Value;
            set => Property = value;
        }

        /// <summary>
        ///     Use to output vtbl offsets for methods as private fields that can be modified
        /// </summary>
        [XmlIgnore]
        public bool? CustomVtbl { get; set; }

        [XmlAttribute("custom-vtbl")]
        public bool _CustomVtbl_
        {
            get => CustomVtbl.Value;
            set => CustomVtbl = value;
        }

        /// <summary>
        ///     Used for property zith COM Objects, in order to persist the getter
        /// </summary>
        [XmlIgnore]
        public bool? Persist { get; set; }

        [XmlAttribute("persist")]
        public bool _Persist_
        {
            get => Persist.Value;
            set => Persist = value;
        }

        /// <summary>
        ///     Gets or sets the struct pack alignment.
        /// </summary>
        /// <value>The struct pack. </value>
        [XmlIgnore]
        public int? StructPack { get; set; }

        [XmlAttribute("pack")]
        public int _StructPack_
        {
            get => StructPack.Value;
            set => StructPack = value;
        }

        /// <summary>
        ///     Mapping name
        /// </summary>
        [XmlAttribute("name-tmp")]
        public string MappingName { get; set; }

        /// <summary>
        ///     Mapping name
        /// </summary>
        [XmlAttribute("name")]
        public string MappingNameFinal
        {
            get => MappingName;
            set
            {
                MappingName = value;
                IsFinalMappingName = true;
            }
        }

        /// <summary>
        ///     True if the MappingName doesn't need any further rename processing
        /// </summary>
        [XmlIgnore]
        public bool? IsFinalMappingName { get; set; }

        /// <summary>
        ///     True if a struct should used a native value type marshalling
        /// </summary>
        [XmlIgnore]
        public bool? StructHasNativeValueType { get; set; }

        [XmlAttribute("native")]
        public bool _StructHasNativeValueType_
        {
            get => StructHasNativeValueType.Value;
            set => StructHasNativeValueType = value;
        }

        /// <summary>
        ///     True if a struct should be used as a class instead of struct (imply StructHasNativeValueType)
        /// </summary>
        [XmlIgnore]
        public bool? StructToClass { get; set; }

        [XmlAttribute("struct-to-class")]
        public bool _StructToClass_
        {
            get => StructToClass.Value;
            set => StructToClass = value;
        }

        /// <summary>
        ///     True if a struct is using some Custom Marshal (imply StructHasNativeValueType)
        /// </summary>
        [XmlIgnore]
        public bool? StructCustomMarshal { get; set; }

        [XmlAttribute("marshal")]
        public bool _StructCustomMarshal_
        {
            get => StructCustomMarshal.Value;
            set => StructCustomMarshal = value;
        }

        /// <summary>
        ///     True if a struct is using some Custom Marshal (imply StructHasNativeValueType)
        /// </summary>
        [XmlIgnore]
        public bool? IsStaticMarshal { get; set; }

        [XmlAttribute("static-marshal")]
        public bool _IsStaticMarshal_
        {
            get => IsStaticMarshal.Value;
            set => IsStaticMarshal = value;
        }

        /// <summary>
        ///     True if a struct is using some a Custom New for the Native struct (imply StructHasNativeValueType)
        /// </summary>
        [XmlIgnore]
        public bool? StructCustomNew { get; set; }

        [XmlAttribute("new")]
        public bool _StructCustomNew_
        {
            get => StructCustomNew.Value;
            set => StructCustomNew = value;
        }

        /// <summary>
        ///     Mapping type name
        /// </summary>
        [XmlAttribute("type")]
        public string MappingType { get; set; }

        /// <summary>
        ///     Set to true to override the type used to natively represent this member when marshalling with the mapping type
        /// </summary>
        [XmlIgnore]
        public bool? OverrideNativeType { get; set; }

        [XmlAttribute("override-native-type")]
        public bool _OverrideNativeType_
        {
            get => OverrideNativeType.Value;
            set => OverrideNativeType = value;
        }

        /// <summary>
        ///     Pointer to modify the type
        /// </summary>
        [XmlAttribute("pointer")]
        public string Pointer { get; set; }

        /// <summary>
        ///     ArrayDimension
        /// </summary>
        [XmlAttribute("array")]
        public string TypeArrayDimension { get; set; }

        /// <summary>
        ///     Used for enums, to tag enums that are used as flags
        /// </summary>
        [XmlIgnore]
        public bool? EnumHasFlags { get; set; }

        [XmlAttribute("flags")]
        public bool _EnumHasFlags_
        {
            get => EnumHasFlags.Value;
            set => EnumHasFlags = value;
        }

        /// <summary>
        ///     Used for enums, to tag enums that should have none value (0)
        /// </summary>
        [XmlIgnore]
        public bool? EnumHasNone { get; set; }

        [XmlAttribute("none")]
        public bool _EnumHasNone_
        {
            get => EnumHasNone.Value;
            set => EnumHasNone = value;
        }

        /// <summary>
        ///     Used for interface to mark them as callback interface
        /// </summary>
        [XmlIgnore]
        public bool? IsCallbackInterface { get; set; }

        [XmlAttribute("callback")]
        public bool _IsCallbackInterface_
        {
            get => IsCallbackInterface.Value;
            set => IsCallbackInterface = value;
        }

        /// <summary>
        ///     Used for interface to mark them as dual-callback interface
        /// </summary>
        [XmlIgnore]
        public bool? IsDualCallbackInterface { get; set; }

        [XmlAttribute("callback-dual")]
        public bool _IsDualCallbackInterface_
        {
            get => IsDualCallbackInterface.Value;
            set => IsDualCallbackInterface = value;
        }

        [XmlIgnore] public bool? AutoGenerateShadow { get; set; }

        [XmlAttribute("autogen-shadow")]
        public bool _AutoGenerateShadow_
        {
            get => AutoGenerateShadow.Value;
            set => AutoGenerateShadow = value;
        }

        [XmlAttribute("shadow-name")] public string ShadowName { get; set; }

        [XmlAttribute("vtbl-name")] public string VtblName { get; set; }

        /// <summary>
        ///     Used for methods to specify that inheriting methods from interface should be kept public and without any rename.
        /// </summary>
        [XmlIgnore]
        public bool? IsKeepImplementPublic { get; set; }

        [XmlAttribute("keep-implement-public")]
        public bool _IsKeepImplementPublic_
        {
            get => IsKeepImplementPublic.Value;
            set => IsKeepImplementPublic = value;
        }

        /// <summary>
        ///     DLL name attached to a function
        /// </summary>
        [XmlAttribute("dll")]
        public string FunctionDllName { get; set; }

        /// <summary>
        ///     Used to duplicate methods taking pointers and generate an additional private method with pure pointer. This method
        ///     is also disabling renaming
        /// </summary>
        /// <value><c>true</c> if [raw PTR]; otherwise, <c>false</c>.</value>
        [XmlIgnore]
        public bool? RawPtr { get; set; }

        [XmlAttribute("rawptr")]
        public bool _RawPtr_
        {
            get => RawPtr.Value;
            set => RawPtr = value;
        }

        /// <summary>
        ///     Parameter Attribute
        /// </summary>
        [XmlIgnore]
        public ParamAttribute? ParameterAttribute { get; set; }

        [XmlAttribute("attribute")]
        public ParamAttribute _ParameterAttribute_
        {
            get => ParameterAttribute.Value;
            set => ParameterAttribute = value;
        }

        /// <summary>
        ///     For Method, true means that the return type should be returned in any case. For Parameter is tagged to be used as a
        ///     return type
        /// </summary>
        [XmlIgnore]
        public bool? ParameterUsedAsReturnType { get; set; }

        [XmlAttribute("return")]
        public bool _ParameterUsedAsReturnType_
        {
            get => ParameterUsedAsReturnType.Value;
            set => ParameterUsedAsReturnType = value;
        }

        /// <summary>
        ///     ClassType attached to a function
        /// </summary>
        [XmlAttribute("group")]
        public string Group { get; set; }

        /// <summary>
        ///     An integer that can be used to transform the method's vtable offset relative to the value specified by the
        ///     compiler.
        /// </summary>
        [XmlAttribute("offset-translate")]
        public int LayoutOffsetTranslate { get; set; }

        /// <summary>
        ///     Specifies how a marshallable element is related to other marshallables.
        /// </summary>
        [XmlAttribute("relation")]
        public string Relation { get; set; }

        /// <summary>
        ///     Provides an ability to prevent method generation in an interface
        /// </summary>
        [XmlIgnore]
        public bool? Hidden { get; set; }

        [XmlAttribute("hidden")]
        public bool _Hidden_
        {
            get => Hidden.Value;
            set => Hidden = value;
        }

        public bool ShouldSerialize_MethodCheckReturnType_() => MethodCheckReturnType != null;

        public bool ShouldSerialize_AlwaysReturnHResult_() => AlwaysReturnHResult != null;

        public bool ShouldSerialize_Visibility_() => Visibility != null;

        public bool ShouldSerialize_NativeCallbackVisibility_() => NativeCallbackVisibility != null;

        public bool ShouldSerialize_NamingFlags_() => NamingFlags != null;

        public bool ShouldSerialize_Property_() => Property != null;

        public bool ShouldSerialize_CustomVtbl_() => CustomVtbl != null;

        public bool ShouldSerialize_Persist_() => Persist != null;

        public bool ShouldSerialize_StructPack_() => StructPack != null;

        public bool ShouldSerializeMappingName() => IsFinalMappingName.HasValue && !IsFinalMappingName.Value;

        public bool ShouldSerializeMappingNameFinal() => !IsFinalMappingName.HasValue || IsFinalMappingName.Value;

        public bool ShouldSerialize_StructHasNativeValueType_() => StructHasNativeValueType != null;

        public bool ShouldSerialize_StructToClass_() => StructToClass != null;

        public bool ShouldSerialize_StructCustomMarshal_() => StructCustomMarshal != null;

        public bool ShouldSerialize_IsStaticMarshal_() => IsStaticMarshal != null;

        public bool ShouldSerialize_StructCustomNew_() => StructCustomNew != null;

        public bool ShouldSerialize_OverrideNativeType_() => OverrideNativeType != null;

        public bool ShouldSerialize_EnumHasFlags_() => EnumHasFlags != null;

        public bool ShouldSerialize_EnumHasNone_() => EnumHasNone != null;

        public bool ShouldSerialize_IsCallbackInterface_() => IsCallbackInterface != null;

        public bool ShouldSerialize_IsDualCallbackInterface_() => IsDualCallbackInterface != null;

        public bool ShouldSerialize_AutoGenerateShadow_() => AutoGenerateShadow != null;

        public bool ShouldSerialize_IsKeepImplementPublic_() => IsKeepImplementPublic != null;

        public bool ShouldSerialize_RawPtr_() => RawPtr != null;

        public bool ShouldSerialize_ParameterAttribute_() => ParameterAttribute != null;

        public bool ShouldSerialize_ParameterUsedAsReturnType_() => ParameterUsedAsReturnType != null;

        public bool ShouldSerialize_Hidden_() => Hidden != null;
    }
}