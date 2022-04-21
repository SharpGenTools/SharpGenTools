using SharpGen.Model;

namespace SharpGen.Transform;

public interface ITransformer<TCsElement>
    where TCsElement: CsBase
{
    void Process(TCsElement csElement);
}