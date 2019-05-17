using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGen.Logging
{
    public static class LoggingCodes
    {
        public const string MissingConfigDependency = "SG0001";

        public const string ConfigNotFound = "SG0002";

        public const string UnknownSdk = "SG0003";

        public const string MissingElementInRule = "SG0004";

        public const string UnableToParseConfig = "SG0005";

        public const string IncludeDirectoryNotFound = "SG0006";

        public const string RegistryKeyNotFound = "SG0007";

        public const string UnkownVariable = "SG0008";

        public const string UnkownDynamicVariable = "SG0009";

        public const string InvalidMethodReturnType = "SG0010";

        public const string InvalidMethodParameterType = "SG0011";

        public const string FunctionNotAttachedToGroup = "SG0012";

        public const string TypeNotDefined = "SG0013";

        public const string DuplicateBinding = "SG0014";

        public const string InvalidUnderlyingType = "SG0015";

        public const string UnknownFundamentalType = "SG0017";

        public const string NonPortableAlignment = "SG0018";

        public const string UnusedMappingRule = "SG0019";

        public const string CannotMarshalUnknownType = "SG0020";

        public const string InvalidRelation = "SG0021";

        public const string InvalidRelationInScenario = "SG0022";

        public const string InvalidPlatformDetectionType = "SG0023";

        public const string CastXmlError = "CX0001";

        public const string CastXmlWarning = "CX0002";

        public const string CastXmlFailed = "CX0003";
    }
}
