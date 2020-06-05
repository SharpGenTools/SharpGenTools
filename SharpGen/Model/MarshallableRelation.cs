using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Transform;

namespace SharpGen.Model
{
    [DataContract(Name = "Marshallable-Relation")]
    public abstract class MarshallableRelation
    {
    }

    [DataContract(Name = "Struct-Size")]
    public sealed class StructSizeRelation : MarshallableRelation
    {
        public override string ToString()
        {
            return "Size of enclosing structure";
        }
    }

    [DataContract(Name = "Length")]
    public sealed class LengthRelation : MarshallableRelation
    {
        [DataMember]
        public string Identifier { get; set; }

        public override string ToString()
        {
            return $"Length of '{Identifier}'";
        }
    }

    [DataContract(Name = "Constant-Value")]
    public sealed class ConstantValueRelation : MarshallableRelation
    {
        [DataMember]
        private string ExpressionString
        {
            get => Value.ToString();
            set => Value = SyntaxFactory.ParseExpression(value, options: RelationParser.SharpParseOptions);
        }

        public ExpressionSyntax Value { get; set; }

        public override string ToString()
        {
            return $"Constant Value '{Value}'";
        }
    }
}