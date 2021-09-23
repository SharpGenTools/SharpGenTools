// Copyright (c) 2010-2014 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Globalization;

namespace SharpGen.Runtime.Win32
{
    /// <summary>
    ///     Implementation of OLE IPropertyBag2.
    /// </summary>
    /// <unmanaged>IPropertyBag2</unmanaged>
    public partial class IPropertyBag2
    {
        /// <summary>
        ///     Gets the number of properties.
        /// </summary>
        public int Count => (int) CountProperties();

        /// <summary>
        ///     Gets the keys.
        /// </summary>
        public string[] Keys
        {
            get
            {
                var count = CountProperties();
                var keys = new string[count];
                var array = new PropertyBagMetadata[1];
                for (uint i = 0; i < count; i++)
                {
                    GetPropertyInfo(i, 1, array, out _);
                    keys[i] = array[0].Name;
                }

                return keys;
            }
        }

        public PropertyBagMetadata[] Properties
        {
            get
            {
                var count = CountProperties();
                var properties = new PropertyBagMetadata[count];
                var array = new PropertyBagMetadata[1];
                for (uint i = 0; i < count; i++)
                {
                    GetPropertyInfo(i, 1, array, out _);
                    properties[i] = array[0];
                }

                return properties;
            }
        }

        /// <summary>
        ///     Gets the value of the property with provided property metadata.
        /// </summary>
        /// <returns>Value of the property</returns>
        public object Get(PropertyBagMetadata metadata)
        {
            var value = new Variant[1];
            var error = new Result[1];

            // Gets the property
            Read(1, new[] { metadata }, null, value, error);
            if (error[0].Failure)
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture, "Property with name [{0}] is not valid for this instance",
                        metadata.Name
                    )
                );

            return value[0].Value;
        }

        /// <summary>
        ///     Gets the value of the property with this name.
        /// </summary>
        /// <returns>Value of the property</returns>
        public object Get(string name) => Get(new PropertyBagMetadata { Name = name });

        /// <summary>
        ///     Gets the value of the property with provided property metadata.
        /// </summary>
        /// <typeparam name="T">The public type of this property.</typeparam>
        /// <returns>Value of the property</returns>
        public T Get<T>(PropertyBagMetadata metadata) => (T) Convert.ChangeType(Get(metadata), typeof(T));

        /// <summary>
        ///     Gets the value of the property with this name.
        /// </summary>
        /// <typeparam name="T">The public type of this property.</typeparam>
        /// <returns>Value of the property</returns>
        public T Get<T>(string name) => (T) Convert.ChangeType(Get(name), typeof(T));

        /// <summary>
        ///     Sets the value of the property with provided property metadata.
        /// </summary>
        /// <typeparam name="T">The public type of this property.</typeparam>
        public void Set<T>(PropertyBagMetadata metadata, T value)
        {
            // In order to set a property in the property bag
            // we need to convert the value to the destination type
            var previousValue = Get(metadata);
            var newValue = previousValue == null
                               ? value
                               : Convert.ChangeType(value, previousValue.GetType());

            // Set the property
            Write(1, new[] { metadata }, new[] { new Variant { Value = newValue } });
        }

        /// <summary>
        ///     Sets the value of the property with this name
        /// </summary>
        /// <typeparam name="T">The public type of this property.</typeparam>
        public void Set<T>(string name, T value) => Set(new PropertyBagMetadata { Name = name }, value);
    }
}