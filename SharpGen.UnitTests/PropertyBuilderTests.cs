using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Model;
using SharpGen.Transform;
using Xunit;

namespace SharpGen.UnitTests
{
    public class PropertyBuilderTests
    {
        [Fact]
        public void MethodWithNameStartingWithIsCreatesProperty()
        {
            var propertyBuilder = new PropertyBuilder(new GlobalNamespaceProvider());

            var returnType = TypeRegistry.Int32;

            var isMethod = new CsMethod(null, "IsActive")
            {
                ReturnValue = new CsReturnValue(null)
                {
                    PublicType = returnType,
                    MarshalType = returnType
                },
            };

            var properties = propertyBuilder.CreateProperties(new[] { isMethod });
            Assert.True(properties.ContainsKey("IsActive"));
            var prop = properties["IsActive"];
            Assert.Equal(returnType, prop.PublicType);
        }

        [Fact]
        public void MethodWithNameStartingWithGetCreatesProperty()
        {
            var propertyBuilder = new PropertyBuilder(new GlobalNamespaceProvider());

            var returnType = TypeRegistry.Int32;

            var getMethod = new CsMethod(null, "GetActive")
            {
                ReturnValue = new CsReturnValue(null)
                {
                    PublicType = returnType,
                    MarshalType = returnType
                },
            };

            var properties = propertyBuilder.CreateProperties(new[] { getMethod });
            Assert.True(properties.ContainsKey("Active"));
            var prop = properties["Active"];
            Assert.Equal(returnType, prop.PublicType);
        }

        [Fact]
        public void MethodWithNameStartingWithSetCreatesProperty()
        {
            var propertyBuilder = new PropertyBuilder(new GlobalNamespaceProvider());

            var paramType = TypeRegistry.Int32;

            var setMethod = new CsMethod(null, "SetActive")
            {
                ReturnValue = new CsReturnValue(null)
                {
                    PublicType = TypeRegistry.Void
                }
            };

            setMethod.Add(new CsParameter(null, null)
            {
                PublicType = paramType
            });

            var properties = propertyBuilder.CreateProperties(new[] { setMethod });
            Assert.True(properties.ContainsKey("Active"));
            var prop = properties["Active"];
            Assert.Equal(paramType, prop.PublicType);
        }

        [Fact]
        public void MethodWithNameStartingWithSetAndReturningResultGeneratesProperty()
        {
            var propertyBuilder = new PropertyBuilder(new GlobalNamespaceProvider());

            var paramType = TypeRegistry.Int32;

            var setMethod = new CsMethod(null, "SetActive")
            {
                ReturnValue = new CsReturnValue(null)
                {
                    PublicType = new CsStruct(null, "SharpGen.Runtime.Result")
                }
            };

            setMethod.Add(new CsParameter(null, null)
            {
                PublicType = paramType
            });

            var properties = propertyBuilder.CreateProperties(new[] { setMethod });
            Assert.True(properties.ContainsKey("Active"), "Property not created");
            var prop = properties["Active"];
            Assert.Equal(paramType, prop.PublicType);
        }

        [Fact]
        public void GetterMethodReturningStatusCodeWithOutParamGeneratesProperty()
        {
            var propertyBuilder = new PropertyBuilder(new GlobalNamespaceProvider());

            var paramType = TypeRegistry.Int32;

            var getMethod = new CsMethod(null, "GetActive")
            {
                ReturnValue = new CsReturnValue(null)
                {
                    PublicType = new CsStruct(null, "SharpGen.Runtime.Result")
                }
            };

            getMethod.Add(new CsParameter(null, null)
            {
                PublicType = paramType,
                Attribute = CsParameterAttribute.Out
            });


            var properties = propertyBuilder.CreateProperties(new[] { getMethod });
            Assert.True(properties.ContainsKey("Active"));
            var prop = properties["Active"];
            Assert.True(prop.IsPropertyParam);
            Assert.Equal(paramType, prop.PublicType);
        }

        [Fact]
        public void GetterWithInvalidSetterDoesNotGenerateProperty()
        {
            var propertyBuilder = new PropertyBuilder(new GlobalNamespaceProvider());

            var returnType = TypeRegistry.Int32;

            var getMethod = new CsMethod(null, "GetActive")
            {
                ReturnValue = new CsReturnValue(null)
                {
                    PublicType = returnType,
                    MarshalType = returnType
                },
            };

            var invalidSetMethod = new CsMethod(null, "SetActive")
            {
                ReturnValue = new CsReturnValue(null)
                {
                    PublicType = TypeRegistry.Void
                }
            };

            Assert.Empty(propertyBuilder.CreateProperties(new[] { getMethod, invalidSetMethod }));

            Assert.Empty(propertyBuilder.CreateProperties(new[] { invalidSetMethod, getMethod }));
        }

        [Fact]
        public void InvalidGetterDoesNotCreateProperty()
        {
            var propertyBuilder = new PropertyBuilder(new GlobalNamespaceProvider());

            var returnType = TypeRegistry.Void;

            var getMethod = new CsMethod(null, "GetActive")
            {
                ReturnValue = new CsReturnValue(null)
                {
                    PublicType = returnType,
                    MarshalType = returnType
                },
            };

            Assert.Empty(propertyBuilder.CreateProperties(new[] { getMethod }));
        }

        [Fact]
        public void InvalidSetterDoesNotCreateProperty()
        {
            var propertyBuilder = new PropertyBuilder(new GlobalNamespaceProvider());

            var setMethod = new CsMethod(null, "SetActive")
            {
                ReturnValue = new CsReturnValue(null)
                {
                    PublicType = TypeRegistry.Void
                }
            };

            Assert.Empty(propertyBuilder.CreateProperties(new[] { setMethod }));
        }

        [Fact]
        public void GeneratePropertyIfGetterAndSetterMatch()
        {
            var propertyBuilder = new PropertyBuilder(new GlobalNamespaceProvider());

            var paramType = TypeRegistry.Int32;

            var getMethod = new CsMethod(null, "GetActive")
            {
                ReturnValue = new CsReturnValue(null)
                {
                    PublicType = paramType,
                    MarshalType = paramType
                },
            };

            var setMethod = new CsMethod(null, "SetActive")
            {
                ReturnValue = new CsReturnValue(null)
                {
                    PublicType = TypeRegistry.Void
                }
            };

            setMethod.Add(new CsParameter(null, null)
            {
                PublicType = paramType
            });

            var props = propertyBuilder.CreateProperties(new[] { getMethod, setMethod });
            Assert.Single(props);
            Assert.NotNull(props["Active"].Getter);
            Assert.NotNull(props["Active"].Setter);
        }

        [Fact]
        public void DoesNotGeneratePropertyIfGetterAndSetterMismatch()
        {
            var propertyBuilder = new PropertyBuilder(new GlobalNamespaceProvider());

            var paramType = TypeRegistry.Int32;

            var getMethod = new CsMethod(null, "GetActive")
            {
                ReturnValue = new CsReturnValue(null)
                {
                    PublicType = paramType,
                    MarshalType = paramType
                },
            };

            var setMethod = new CsMethod(null, "SetActive")
            {
                ReturnValue = new CsReturnValue(null)
                {
                    PublicType = TypeRegistry.Void
                }
            };

            setMethod.Add(new CsParameter(null, null)
            {
                PublicType = TypeRegistry.Int16
            });

            var props = propertyBuilder.CreateProperties(new[] { getMethod, setMethod });
            Assert.Empty(props);
        }

        [Fact]
        public void DoesNotGeneratePropertyIfGetterAndSetterMismatch_ParameterizedGetter()
        {
            var propertyBuilder = new PropertyBuilder(new GlobalNamespaceProvider());

            var paramType = TypeRegistry.Int32;

            var getMethod = new CsMethod(null, "GetActive")
            {
                ReturnValue = new CsReturnValue(null)
                {
                    PublicType = new CsStruct(null, "SharpGen.Runtime.Result")
                }
            };

            getMethod.Add(new CsParameter(null, null)
            {
                PublicType = paramType,
                Attribute = CsParameterAttribute.Out
            });

            var setMethod = new CsMethod(null, "SetActive")
            {
                ReturnValue = new CsReturnValue(null)
                {
                    PublicType = TypeRegistry.Void
                }
            };

            setMethod.Add(new CsParameter(null, null)
            {
                PublicType = TypeRegistry.Int16
            });

            var props = propertyBuilder.CreateProperties(new[] { getMethod, setMethod });
            Assert.Empty(props);
        }

        [Fact]
        public void DoesNotGeneratePropertyIfOverloaded()
        {
            var propertyBuilder = new PropertyBuilder(new GlobalNamespaceProvider());

            var paramType = TypeRegistry.Int32;

            var getMethod = new CsMethod(null, "GetActive")
            {
                ReturnValue = new CsReturnValue(null)
                {
                    PublicType = paramType,
                    MarshalType = paramType
                },
            };

            var getMethodOverload = new CsMethod(null, "GetActive")
            {
                ReturnValue = new CsReturnValue(null)
                {
                    PublicType = paramType,
                    MarshalType = paramType
                },
            };

            var setMethod = new CsMethod(null, "SetActive")
            {
                ReturnValue = new CsReturnValue(null)
                {
                    PublicType = TypeRegistry.Void
                }
            };

            setMethod.Add(new CsParameter(null, null)
            {
                PublicType = paramType
            });

            Assert.Empty(propertyBuilder.CreateProperties(new[] { getMethod, setMethod, getMethodOverload }));
            Assert.Empty(propertyBuilder.CreateProperties(new[] { getMethod, getMethodOverload, setMethod }));
        }

        [Fact]
        public void PropertyAttachedToGetterType()
        {
            var paramType = TypeRegistry.Int32;

            CppMethod cppGetMethod = new("GetActive")
            {
                Rule =
                {
                    Property = true
                }
            };

            var getMethod = new CsMethod(cppGetMethod, cppGetMethod.Name)
            {
                ReturnValue = new CsReturnValue(null)
                {
                    PublicType = paramType,
                    MarshalType = paramType
                }
            };

            var iface = new CsInterface(null, null);
            iface.Add(getMethod);

            var prop = new CsProperty(null, "Active", getMethod, null);

            PropertyBuilder.AttachPropertyToParent(prop);

            Assert.Equal(iface, prop.Parent);
        }

        [Fact]
        public void SetOnlyPropertyAttachedToSetterType()
        {
            CppMethod cppSetMethod = new("SetActive")
            {
                Rule =
                {
                    Property = true
                }
            };

            var paramType = TypeRegistry.Int32;

            var setMethod = new CsMethod(cppSetMethod, cppSetMethod.Name)
            {
                ReturnValue = new CsReturnValue(null)
                {
                    PublicType = TypeRegistry.Void
                }
            };

            setMethod.Add(new CsParameter(null, null)
            {
                PublicType = paramType
            });

            var iface = new CsInterface(null, null);
            iface.Add(setMethod);

            var prop = new CsProperty(null, "Active", null, setMethod);

            PropertyBuilder.AttachPropertyToParent(prop);

            Assert.Equal(iface, prop.Parent);
        }

        [Fact]
        public void PropertyNotAttachedWhenGetterAllowPropertyIsFalse()
        {
            CppMethod cppGetMethod = new("GetActive")
            {
                Rule =
                {
                    Property = false
                }
            };

            var paramType = TypeRegistry.Int32;

            var getMethod = new CsMethod(cppGetMethod, cppGetMethod.Name)
            {
                ReturnValue = new CsReturnValue(null)
                {
                    PublicType = paramType,
                    MarshalType = paramType
                }
            };

            var iface = new CsInterface(null, null);
            iface.Add(getMethod);

            var prop = new CsProperty(null, "Active", getMethod, null);

            PropertyBuilder.AttachPropertyToParent(prop);

            Assert.Null(prop.Parent);
        }

        [Fact]
        public void PropertyNotAttachedWhenSetterAllowPropertyIsFalse()
        {
            CppMethod cppSetMethod = new("SetActive")
            {
                Rule =
                {
                    Property = false
                }
            };

            var paramType = TypeRegistry.Int32;

            var setMethod = new CsMethod(cppSetMethod, cppSetMethod.Name)
            {
                ReturnValue = new CsReturnValue(null)
                {
                    PublicType = TypeRegistry.Void
                }
            };

            setMethod.Add(new CsParameter(null, null)
            {
                PublicType = paramType
            });

            var iface = new CsInterface(null, null);
            iface.Add(setMethod);

            var prop = new CsProperty(null, "Active", null, setMethod);

            PropertyBuilder.AttachPropertyToParent(prop);

            Assert.Null(prop.Parent);
        }

        [Fact]
        public void PersistentGetterGeneratesPersistentProperty()
        {
            CppMethod cppGetMethod = new("GetActive")
            {
                Rule =
                {
                    Property = true,
                    Persist = true
                }
            };

            var paramType = TypeRegistry.Int32;

            var getMethod = new CsMethod(cppGetMethod, cppGetMethod.Name)
            {
                ReturnValue = new CsReturnValue(null)
                {
                    PublicType = paramType,
                    MarshalType = paramType
                }
            };

            var iface = new CsInterface(null, null);
            iface.Add(getMethod);

            var prop = new CsProperty(null, "Active", getMethod, null);

            PropertyBuilder.AttachPropertyToParent(prop);

            Assert.True(prop.IsPersistent);
        }

        [Fact]
        public void GetterVisibiltyInternal()
        {
            CppMethod cppGetMethod = new("GetActive")
            {
                Rule =
                {
                    Property = true
                }
            };

            var paramType = TypeRegistry.Int32;

            var getMethod = new CsMethod(cppGetMethod, cppGetMethod.Name)
            {
                ReturnValue = new CsReturnValue(null)
                {
                    PublicType = paramType,
                    MarshalType = paramType
                }
            };

            var iface = new CsInterface(null, null);
            iface.Add(getMethod);

            var prop = new CsProperty(null, "Active", getMethod, null);

            PropertyBuilder.AttachPropertyToParent(prop);

            Assert.Equal(Visibility.Internal, getMethod.Visibility);
        }

        [Fact]
        public void SetterVisibilityInternal()
        {
            CppMethod cppSetMethod = new("SetActive")
            {
                Rule =
                {
                    Property = true
                }
            };

            var paramType = TypeRegistry.Int32;

            var setMethod = new CsMethod(cppSetMethod, cppSetMethod.Name)
            {
                ReturnValue = new CsReturnValue(null)
                {
                    PublicType = TypeRegistry.Void
                }
            };

            setMethod.Add(new CsParameter(null, null)
            {
                PublicType = paramType
            });

            var iface = new CsInterface(null, null);
            iface.Add(setMethod);

            var prop = new CsProperty(null, "Active", null, setMethod);

            PropertyBuilder.AttachPropertyToParent(prop);

            Assert.Equal(Visibility.Internal, setMethod.Visibility);
        }

        [Fact]
        public void NonPropertyMethodWithNonPropertyNameShouldNotCreateProperty()
        {
            var propertyBuilder = new PropertyBuilder(new GlobalNamespaceProvider());

            var setMethod = new CsMethod(null, "MyMethod")
            {
                ReturnValue = new CsReturnValue(null)
                {
                    PublicType = TypeRegistry.Void
                }
            };

            Assert.Empty(propertyBuilder.CreateProperties(new[] { setMethod }));
        }
    }
}
