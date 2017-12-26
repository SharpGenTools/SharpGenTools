using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGen.Generator
{
    public class GroupRegistry
    {
        private readonly Dictionary<string, CsClass> _mapRegisteredFunctionGroup = new Dictionary<string, CsClass>();

        public void RegisterGroup(string className, CsClass group)
        {
            _mapRegisteredFunctionGroup.Add(className, group);
        }

        /// <summary>
        /// Finds a C# class container by name.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        /// <returns></returns>
        public CsClass FindGroup(string className)
        {
            _mapRegisteredFunctionGroup.TryGetValue(className, out CsClass csClass);
            return csClass;
        }

    }
}
