using System;
using System.Xml.Serialization;

namespace SharpGen.Config
{
    public enum SdkLib
    {
        StdLib = 1,
        WindowsSdk
    }

    public static class SdkLibExtensions
    {
        public static string Name(this SdkLib lib) => lib switch
        {
            SdkLib.StdLib => "standard library",
            SdkLib.WindowsSdk => "Windows SDK",
            _ => lib.ToString()
        };
    }

    public class SdkRule
    {
        public SdkRule()
        {
        }

        public SdkRule(SdkLib name, string version)
        {
            Name = name;
            Version = version;
        }

        [XmlIgnore]
        public SdkLib? Name { get; private set; }

        [XmlAttribute("name")]
        public string _Name_
        {
            get => Name.ToString();
            set
            {
                if (Enum.TryParse(value, out SdkLib name))
                    Name = name;
                else
                    Name = null;
            }
        }

        public bool ShouldSerialize_Name_() => Name is { } name && Enum.IsDefined(typeof(SdkLib), name);

        [XmlAttribute("version")]
        public string Version { get; set; }

        [XmlAttribute("components")]
        public string Components { get; set; }
    }
}
