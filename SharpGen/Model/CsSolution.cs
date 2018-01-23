using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace SharpGen.Model
{
    [DataContract(Name = "Solution")]
    public class CsSolution : CsBase
    {
        public IEnumerable<CsAssembly> Assemblies => Items.OfType<CsAssembly>();

        /// <summary>
        /// Reads the module from the specified file.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns>A C++ module</returns>
        public static CsSolution Read(string file)
        {
            using (var input = new FileStream(file, FileMode.Open))
            {
                return Read(input);
            }
        }

        /// <summary>
        /// Reads the module from the specified input.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>A C++ module</returns>
        public static CsSolution Read(Stream input)
        {
            var ds = GetSerializer();

            CsSolution solution = null;
            using (XmlReader w = XmlReader.Create(input))
            {
                solution = ds.ReadObject(w) as CsSolution;
            }

            return solution;
        }

        private static DataContractSerializer GetSerializer()
        {
            var knownTypes = new[]
            {
                        typeof(CsAssembly),
                        typeof(CsNamespace),
                        typeof(CsInterface),
                        typeof(CsGroup),
                        typeof(CsStruct),
                        typeof(CsInterfaceArray),
                        typeof(CsEnum),
                        typeof(CsEnumItem),
                        typeof(CsFunction),
                        typeof(CsMethod),
                        typeof(CsField),
                        typeof(CsParameter),
                        typeof(CsProperty),
                        typeof(CsVariable),
                        typeof(CsTypeBase),
                        typeof(CsReturnValue),
                        typeof(CsMarshalBase),
                        typeof(CsFundamentalType),
                        typeof(CsUndefinedType)
            };

            return new DataContractSerializer(typeof(CsSolution), new DataContractSerializerSettings
            {
                KnownTypes = knownTypes,
                PreserveObjectReferences = true
            });
        }

        private static XmlSerializer GetXmlSerializer()
        {
            return new XmlSerializer(
                typeof(CsSolution),
                new[]
                {
                    typeof(CsAssembly),
                    typeof(CsNamespace),
                    typeof(CsInterface),
                    typeof(CsGroup),
                    typeof(CsStruct),
                    typeof(CsInterfaceArray),
                    typeof(CsEnum),
                    typeof(CsEnumItem),
                    typeof(CsFunction),
                    typeof(CsMethod),
                    typeof(CsField),
                    typeof(CsParameter),
                    typeof(CsProperty),
                    typeof(CsVariable)
                }
            );
        }

        /// <summary>
        /// Writes this instance to the specified file.
        /// </summary>
        /// <param name="file">The file.</param>
        public void Write(string file)
        {
            using (var output = new FileStream(file, FileMode.Create))
            {
                Write(output);
            }
        }

        /// <summary>
        /// Writes this instance to the specified output.
        /// </summary>
        /// <param name="output">The output.</param>
        public void Write(Stream output)
        {
            var ds = GetSerializer();

            var settings = new XmlWriterSettings { Indent = true };
            using (XmlWriter w = XmlWriter.Create(output, settings))
            {
                ds.WriteObject(w, this);
            }
        }
    }
}
