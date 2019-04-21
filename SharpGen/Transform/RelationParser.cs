using SharpGen.Logging;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGen.Transform
{
    static class RelationParser
    {
        public static MarshallableRelation ParseRelation(string relation, Logger logger)
        {
            if (string.IsNullOrWhiteSpace(relation))
            {
                return null;
            }

            if (relation == "struct-size()")
            {
                return new StructSizeRelation();
            }
            else if (relation.StartsWith("const(") && relation.EndsWith(")"))
            {
                return new ConstantValueRelation
                {
                    Value = relation.Substring("const(".Length, relation.Length - "const(".Length - 1)
                };
            }
            else if (relation.StartsWith("length(") && relation.EndsWith(")"))
            {
                return new LengthRelation
                {
                    RelatedMarshallableName = relation.Substring("length(".Length, relation.Length - "length(".Length - 1)
                };
            }

            logger.Error(LoggingCodes.InvalidRelation, $"Relation [{relation}] unknown. Ignoring.");
            return null;
        }
    }
}
