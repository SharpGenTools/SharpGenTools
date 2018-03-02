using SharpGen.Config;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SharpGen.Transform
{
    public enum PropertyMethod
    {
        Getter,
        Setter
    }

    public class PropertyBuilder
    {
        private static readonly Regex MatchGet = new Regex(@"^\s*(\<[Pp]\>)?\s*(Gets?|Retrieves?|Returns)");
        private readonly GlobalNamespaceProvider globalNamespace;

        public PropertyBuilder(GlobalNamespaceProvider globalNamespace)
        {
            this.globalNamespace = globalNamespace;
        }
        
        public CsProperty CreateProperty(CsMethod csMethod)
        {
            var (propertyName, _) = GetPropertySpec(csMethod);
            var prop = new CsProperty(propertyName);

            if (UpdateOrMarkPropertyInvalid(csMethod, prop))
            {
                return prop;
            }
            return null;
        }

        public bool UpdateOrMarkPropertyInvalid(CsMethod csMethod, CsProperty partiallyBuiltProperty)
        {
            var (propertyName, propertyMethodType) = GetPropertySpec(csMethod);

            if (propertyMethodType == null)
                return false;

            var parameterList = csMethod.Parameters;
            int parameterCount = csMethod.Parameters.Count;

            CsProperty csProperty = partiallyBuiltProperty;

            if (partiallyBuiltProperty == null)
            {
                csProperty = new CsProperty(propertyName);
            }

            // If the property has already a getter and a setter, this must be an error, remove the property
            // (Should never happen, unless there are some polymorphism on the interface's methods)
            if (csProperty.Getter != null && csProperty.Setter != null)
            {
                return true;
            }

            // Check Getter
            if (propertyMethodType == PropertyMethod.Getter)
            {
                if ((csMethod.ReturnValue.PublicType.Name == globalNamespace.GetTypeName(WellKnownName.Result) || !csMethod.HasReturnType) && parameterCount == 1 &&
                    parameterList[0].IsOut && !parameterList[0].IsArray)
                {
                    csProperty.Getter = csMethod;
                    csProperty.PublicType = parameterList[0].PublicType;
                    csProperty.IsPropertyParam = true;
                }
                else if (parameterCount == 0 && csMethod.HasReturnType)
                {
                    csProperty.Getter = csMethod;
                    csProperty.PublicType = csProperty.Getter.ReturnValue.PublicType;
                }
                else
                {
                    // If there is a getter, but the setter is not valid, then remove the getter
                    if (csProperty.Setter != null)
                        return true;
                    return false;
                }
            }
            else
            {
                // Check Setter
                if ((csMethod.ReturnValue?.Name == globalNamespace.GetTypeName(WellKnownName.Result) || !csMethod.HasReturnType) && parameterCount == 1 &&
                    (parameterList[0].IsRefIn || parameterList[0].IsIn || parameterList[0].IsRef) && !parameterList[0].IsArray)
                {
                    csProperty.Setter = csMethod;
                    csProperty.PublicType = parameterList[0].PublicType;
                }
                else if (parameterCount == 1 && !csMethod.HasReturnType)
                {
                    csProperty.Setter = csMethod;
                    csProperty.PublicType = csProperty.Setter.ReturnValue.PublicType;
                }
                else
                {
                    // If there is a getter, but the setter is not valid, then remove the getter
                    if (csProperty.Getter != null)
                        return true;
                    return false;
                }
            }

            // Check when Setter and Getter together that they have the same return type
            if (csProperty.Setter != null && csProperty.Getter != null)
            {
                bool removeProperty = false;

                if (csProperty.IsPropertyParam)
                {
                    var getterParameter = csProperty.Getter.Parameters.First();
                    var setterParameter = csProperty.Setter.Parameters.First();
                    if (getterParameter.PublicType.QualifiedName != setterParameter.PublicType.QualifiedName)
                    {
                        removeProperty = true;
                    }
                }
                else
                {
                    var getterType = csProperty.Getter.ReturnValue;
                    var setterType = csProperty.Setter.Parameters.First();
                    if (getterType.PublicType.QualifiedName != setterType.PublicType.QualifiedName)
                        removeProperty = true;
                }
                if (removeProperty)
                {
                    return true;
                }
            }

            return false;
        }

        public (string name, PropertyMethod? propMethod) GetPropertySpec(CsMethod csMethod)
        {
            var isIs = csMethod.Name.StartsWith("Is");
            var isGet = csMethod.Name.StartsWith("Get") || isIs;
            var isSet = csMethod.Name.StartsWith("Set");

            var propertyName = isIs ? csMethod.Name : csMethod.Name.Substring("Get".Length);
            return (propertyName, isGet ? PropertyMethod.Getter : isSet ? PropertyMethod.Setter : (PropertyMethod?)null);
        }

        public void AttachPropertyToParent(CsProperty property)
        {
            var getterOrSetter = property.Getter ?? property.Setter;

            // Associate the property with the Getter element
            property.CppElement = getterOrSetter.CppElement;

            // If We have a getter, then we need to modify the documentation in order to print that we have Gets and Sets.
            if (property.Getter != null && property.Setter != null && !string.IsNullOrEmpty(property.Description))
            {
                property.Description = MatchGet.Replace(property.Description, "$1$2 or sets");
            }

            var parent = getterOrSetter.Parent;

            // If mapping rule disallows properties, don't attach the property to the model.
            if ((property.Getter != null && !property.Getter.AllowProperty) || (property.Setter != null && !property.Setter.AllowProperty))
                return;

            // Update visibility for getter and setter (set to internal)
            if (property.Getter != null)
            {
                property.Getter.Visibility = Visibility.Internal;
                property.IsPersistent = property.Getter.IsPersistent;
            }

            if (property.Setter != null)
                property.Setter.Visibility = Visibility.Internal;

            if (property.Getter != null && property.Name.StartsWith("Is"))
                property.Getter.Name += "_";

            parent.Add(property);
        }
    }
}
