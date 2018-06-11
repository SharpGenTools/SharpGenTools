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

        public Dictionary<string, CsProperty> CreateProperties(IEnumerable<CsMethod> methods)
        {
            return methods
                .Where(method => GetPropertySpec(method).PropertyMethod != null)
                .GroupBy(method => GetPropertySpec(method).PropertyName)
                .Select(group => (group.Key, Property: CreatePropertyFromMethodGroup(group)))
                .Where(group => group.Property != null)
                .ToDictionary(group => group.Key, group => group.Property);
        }

        private CsProperty CreatePropertyFromMethodGroup(IGrouping<string, CsMethod> group)
        {
            var getters = group.Where(method => GetPropertySpec(method).PropertyMethod == PropertyMethod.Getter).ToList();
            var setters = group.Where(method => GetPropertySpec(method).PropertyMethod == PropertyMethod.Setter).ToList();
            if (getters.Count == 0 && setters.Count == 0)
            {
                return null;
            }
            if (getters.Count > 1 || setters.Count > 1)
            {
                return null;
            }

            var getter = getters.Count == 0 ? null : getters[0];
            var setter = setters.Count == 0 ? null : setters[0];

            var getterValid = ValidateGetter(getter);
            var setterValid = ValidateSetter(setter);

            if (!getterValid || !setterValid)
            {
                return null;
            }

            var isParamGetter = getterValid && getter != null && getter.Parameters.Count == 1;
            var getterPropType = isParamGetter ? getter.Parameters[0].PublicType : getter?.ReturnValue.PublicType;
            var setterPropType = setter?.Parameters[0].PublicType;

            var typesMatch = getterPropType == null || setterPropType == null || getterPropType == setterPropType;

            if (!typesMatch)
            {
                return null;
            }

            return new CsProperty(group.Key)
            {
                Getter = getter,
                Setter = setter,
                PublicType = getterPropType ?? setterPropType,
                IsPropertyParam = isParamGetter
            };
        }

        private bool ValidateGetter(CsMethod getter)
        {
            if (getter == null)
            {
                return true;
            }

            if ((getter.ReturnValue.PublicType.Name == globalNamespace.GetTypeName(WellKnownName.Result) || !getter.HasReturnType)
                && getter.Parameters.Count == 1
                && getter.Parameters[0].IsOut && !getter.Parameters[0].IsArray)
            {
                return true;
            }
            else if (getter.Parameters.Count == 0 && getter.HasReturnType)
            {
                return true;
            }
            return false;
        }

        private bool ValidateSetter(CsMethod setter)
        {
            if (setter == null)
            {
                return true;
            }

            return (setter.ReturnValue?.PublicType.QualifiedName == globalNamespace.GetTypeName(WellKnownName.Result) || !setter.HasReturnType)
                && setter.Parameters.Count == 1
                && (setter.Parameters[0].IsRefIn
                    || setter.Parameters[0].IsIn
                    || setter.Parameters[0].IsRef)
                && !setter.Parameters[0].IsArray;
        }
        
        private (string PropertyName, PropertyMethod? PropertyMethod) GetPropertySpec(CsMethod csMethod)
        {
            var isIs = csMethod.Name.StartsWith("Is");
            var isGet = csMethod.Name.StartsWith("Get") || isIs;
            var isSet = csMethod.Name.StartsWith("Set");

            var propertyName = isIs ? csMethod.Name : csMethod.Name.Substring("Get".Length);
            return (propertyName, isGet ? PropertyMethod.Getter : isSet ? PropertyMethod.Setter : (PropertyMethod?)null);
        }

        public void AttachPropertyToParent(CsProperty property)
        {
            var underlyingMethod = property.Getter ?? property.Setter;

            // Associate the property with the underlying method's C++ element.
            property.CppElement = underlyingMethod.CppElement;

            // If We have a getter, then we need to modify the documentation in order to print that we have Gets and Sets.
            if (property.Getter != null && property.Setter != null && !string.IsNullOrEmpty(property.Description))
            {
                property.Description = MatchGet.Replace(property.Description, "$1$2 or sets");
            }

            var parent = underlyingMethod.Parent;

            // If mapping rule disallows properties, don't attach the property to the model.
            if ((property.Getter?.AllowProperty == false) || (property.Setter?.AllowProperty == false))
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
