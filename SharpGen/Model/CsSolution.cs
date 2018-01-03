using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace SharpGen.Model
{
    [XmlRoot("solution")]
    public class CsSolution : CsBase
    {
        public IEnumerable<CsAssembly> Assemblies => Items.OfType<CsAssembly>();
    }
}
