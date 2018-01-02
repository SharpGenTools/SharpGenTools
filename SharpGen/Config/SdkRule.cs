using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace SharpGen.Config
{
    public enum SdkLib
    {
        StdLib = 1,
        WindowsSdk
    }

    public class SdkRule
    {
        public SdkRule()
        {
        }

        public SdkRule(SdkLib name, Version version)
        {
            Name = name;
            Version = version;
        }

        [XmlIgnore]
        public SdkLib Name { get; private set; }

        [XmlAttribute("name")]
        public string _Name_
        {
            get => Name.ToString();
            set
            {
                Enum.TryParse(value, out SdkLib name);
                Name = name;
            }
        }

        public bool ShouldSerialize_Name_() => Enum.IsDefined(typeof(SdkLib), Name);

        [XmlIgnore]
        public Version Version { get; private set; }

        [XmlAttribute("version")]
        public string _Version_
        {
            get => Version.ToString();
            set => Version = Version.Parse(value);
        }

        public bool ShouldSerialize_Version_() => Version != null;
    }
}
