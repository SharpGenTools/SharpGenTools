using System.Collections.Generic;
using SharpGen.Config;

namespace SharpGen.Transform
{
    public class CheckFileNode
    {
        private List<ConfigFile> _configFilesLinked;

        public CheckFileNode(string name)
        {
            _configFilesLinked = new List<ConfigFile>();
            Name = name;
        }
                
        /// <summary>
        /// Gets config files linked to this assembly
        /// </summary>
        /// <value>The config files linked to this assembly.</value>
        public IReadOnlyList<ConfigFile> ConfigFilesLinked => _configFilesLinked;

        /// <summary>
        /// Adds linked config file to this instance.
        /// </summary>
        /// <param name="configFileToAdd">The config file to add.</param>
        public void AddLinkedConfigFile(ConfigFile configFileToAdd)
        {
            foreach (var configFile in _configFilesLinked)
                if (configFile.Id == configFileToAdd.Id)
                    return;

            _configFilesLinked.Add(configFileToAdd);
        }

        /// <summary>
        /// Gets the name of the check file for this assembly.
        /// </summary>
        /// <value>The name of the check file.</value>
        public string CheckFileName
        {
            get { return Name + ".check"; }
        }

        public string Name { get; }

        public bool NeedsToBeUpdated { get; set; }
    }
}