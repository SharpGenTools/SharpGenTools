using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Model;
using SharpGen.Transform;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.UnitTests
{
    public class PropertyBuilderTests : TestBase
    {
        private readonly PropertyBuilder propertyBuilder;

        public PropertyBuilderTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            propertyBuilder = new PropertyBuilder(Ioc);
        }

        [Fact]
        public void MethodWithNameStartingWithIsCreatesProperty()
        {
            var returnType = TypeRegistry.Int32;

            CsMethod isMethod = new(Ioc, null, "IsActive")
            {
                ReturnValue = new CsReturnValue(Ioc, null)
                {
                    PublicType = returnType,
                    MarshalType = returnType
                }
            };

            var properties = propertyBuilder.CreateProperties(new[] { isMethod });
            Assert.True(properties.ContainsKey("IsActive"));
            var prop = properties["IsActive"];
            Assert.Equal(returnType, prop.PublicType);
        }

        [Fact]
        public void MethodWithNameStartingWithGetCreatesProperty()
        {
            var returnType = TypeRegistry.Int32;

            CsMethod getMethod = new(Ioc, null, "GetActive")
            {
                ReturnValue = new CsReturnValue(Ioc, null)
                {
                    PublicType = returnType,
                    MarshalType = returnType
                }
            };

            var properties = propertyBuilder.CreateProperties(new[] { getMethod });
            Assert.True(properties.ContainsKey("Active"));
            var prop = properties["Active"];
            Assert.Equal(returnType, prop.PublicType);
        }

        [Fact]
        public void MethodWithNameStartingWithSetCreatesProperty()
        {
            var paramType = TypeRegistry.Int32;

            CsMethod setMethod = new(Ioc, null, "SetActive")
            {
                ReturnValue = new CsReturnValue(Ioc, null)
                {
                    PublicType = TypeRegistry.Void
                }
            };

            setMethod.Add(new CsParameter(Ioc, null, null)
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
            var paramType = TypeRegistry.Int32;

            CsMethod setMethod = new(Ioc, null, "SetActive")
            {
                ReturnValue = new CsReturnValue(Ioc, null)
                {
                    PublicType = new CsStruct(null, "SharpGen.Runtime.Result")
                }
            };

            setMethod.Add(new CsParameter(Ioc, null, null)
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
            var paramType = TypeRegistry.Int32;

            CsMethod getMethod = new(Ioc, null, "GetActive")
            {
                ReturnValue = new CsReturnValue(Ioc, null)
                {
                    PublicType = new CsStruct(null, "SharpGen.Runtime.Result")
                }
            };

            getMethod.Add(new CsParameter(Ioc, null, null)
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
            var returnType = TypeRegistry.Int32;

            CsMethod getMethod = new(Ioc, null, "GetActive")
            {
                ReturnValue = new CsReturnValue(Ioc, null)
                {
                    PublicType = returnType,
                    MarshalType = returnType
                }
            };

            var invalidSetMethod = new CsMethod(Ioc, null, "SetActive")
            {
                ReturnValue = new CsReturnValue(Ioc, null)
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
            var returnType = TypeRegistry.Void;

            CsMethod getMethod = new(Ioc, null, "GetActive")
            {
                ReturnValue = new CsReturnValue(Ioc, null)
                {
                    PublicType = returnType,
                    MarshalType = returnType
                }
            };

            Assert.Empty(propertyBuilder.CreateProperties(new[] { getMethod }));
        }

        [Fact]
        public void InvalidSetterDoesNotCreateProperty()
        {
            CsMethod setMethod = new(Ioc, null, "SetActive")
            {
                ReturnValue = new CsReturnValue(Ioc, null)
                {
                    PublicType = TypeRegistry.Void
                }
            };

            Assert.Empty(propertyBuilder.CreateProperties(new[] { setMethod }));
        }

        [Fact]
        public void GeneratePropertyIfGetterAndSetterMatch()
        {
            var paramType = TypeRegistry.Int32;

            CsMethod getMethod = new(Ioc, null, "GetActive")
            {
                ReturnValue = new CsReturnValue(Ioc, null)
                {
                    PublicType = paramType,
                    MarshalType = paramType
                }
            };

            CsMethod setMethod = new(Ioc, null, "SetActive")
            {
                ReturnValue = new CsReturnValue(Ioc, null)
                {
                    PublicType = TypeRegistry.Void
                }
            };

            setMethod.Add(new CsParameter(Ioc, null, null)
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
            var paramType = TypeRegistry.Int32;

            CsMethod getMethod = new(Ioc, null, "GetActive")
            {
                ReturnValue = new CsReturnValue(Ioc, null)
                {
                    PublicType = paramType,
                    MarshalType = paramType
                }
            };

            CsMethod setMethod = new(Ioc, null, "SetActive")
            {
                ReturnValue = new CsReturnValue(Ioc, null)
                {
                    PublicType = TypeRegistry.Void
                }
            };

            setMethod.Add(new CsParameter(Ioc, null, null)
            {
                PublicType = TypeRegistry.Int16
            });

            var props = propertyBuilder.CreateProperties(new[] { getMethod, setMethod });
            Assert.Empty(props);
        }

        [Fact]
        public void DoesNotGeneratePropertyIfGetterAndSetterMismatch_ParameterizedGetter()
        {
            var paramType = TypeRegistry.Int32;

            CsMethod getMethod = new(Ioc, null, "GetActive")
            {
                ReturnValue = new CsReturnValue(Ioc, null)
                {
                    PublicType = new CsStruct(null, "SharpGen.Runtime.Result")
                }
            };

            getMethod.Add(new CsParameter(Ioc, null, null)
            {
                PublicType = paramType,
                Attribute = CsParameterAttribute.Out
            });

            CsMethod setMethod = new(Ioc, null, "SetActive")
            {
                ReturnValue = new CsReturnValue(Ioc, null)
                {
                    PublicType = TypeRegistry.Void
                }
            };

            setMethod.Add(new CsParameter(Ioc, null, null)
            {
                PublicType = TypeRegistry.Int16
            });

            var props = propertyBuilder.CreateProperties(new[] { getMethod, setMethod });
            Assert.Empty(props);
        }

        [Fact]
        public void DoesNotGeneratePropertyIfOverloaded()
        {
            var paramType = TypeRegistry.Int32;

            CsMethod getMethod = new(Ioc, null, "GetActive")
            {
                ReturnValue = new CsReturnValue(Ioc, null)
                {
                    PublicType = paramType,
                    MarshalType = paramType
                }
            };

            CsMethod getMethodOverload = new(Ioc, null, "GetActive")
            {
                ReturnValue = new CsReturnValue(Ioc, null)
                {
                    PublicType = paramType,
                    MarshalType = paramType
                },
            };

            CsMethod setMethod = new(Ioc, null, "SetActive")
            {
                ReturnValue = new CsReturnValue(Ioc, null)
                {
                    PublicType = TypeRegistry.Void
                }
            };

            setMethod.Add(new CsParameter(Ioc, null, null)
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

            CsMethod getMethod = new(Ioc, cppGetMethod, cppGetMethod.Name)
            {
                ReturnValue = new CsReturnValue(Ioc, null)
                {
                    PublicType = paramType,
                    MarshalType = paramType
                }
            };

            var iface = new CsInterface(null, null);
            iface.Add(getMethod);

            CsProperty prop = new(Ioc, null, "Active", getMethod, null);

            propertyBuilder.AttachPropertyToParent(prop);

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

            CsMethod setMethod = new(Ioc, cppSetMethod, cppSetMethod.Name)
            {
                ReturnValue = new CsReturnValue(Ioc, null)
                {
                    PublicType = TypeRegistry.Void
                }
            };

            setMethod.Add(new CsParameter(Ioc, null, null)
            {
                PublicType = paramType
            });

            var iface = new CsInterface(null, null);
            iface.Add(setMethod);

            CsProperty prop = new(Ioc, null, "Active", null, setMethod);

            propertyBuilder.AttachPropertyToParent(prop);

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

            CsMethod getMethod = new(Ioc, cppGetMethod, cppGetMethod.Name)
            {
                ReturnValue = new CsReturnValue(Ioc, null)
                {
                    PublicType = paramType,
                    MarshalType = paramType
                }
            };

            var iface = new CsInterface(null, null);
            iface.Add(getMethod);

            CsProperty prop = new(Ioc, null, "Active", getMethod, null);

            propertyBuilder.AttachPropertyToParent(prop);

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

            CsMethod setMethod = new(Ioc, cppSetMethod, cppSetMethod.Name)
            {
                ReturnValue = new CsReturnValue(Ioc, null)
                {
                    PublicType = TypeRegistry.Void
                }
            };

            setMethod.Add(new CsParameter(Ioc, null, null)
            {
                PublicType = paramType
            });

            var iface = new CsInterface(null, null);
            iface.Add(setMethod);

            CsProperty prop = new(Ioc, null, "Active", null, setMethod);

            propertyBuilder.AttachPropertyToParent(prop);

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

            CsMethod getMethod = new(Ioc, cppGetMethod, cppGetMethod.Name)
            {
                ReturnValue = new CsReturnValue(Ioc, null)
                {
                    PublicType = paramType,
                    MarshalType = paramType
                }
            };

            var iface = new CsInterface(null, null);
            iface.Add(getMethod);

            CsProperty prop = new(Ioc, null, "Active", getMethod, null);

            propertyBuilder.AttachPropertyToParent(prop);

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

            CsMethod getMethod = new(Ioc, cppGetMethod, cppGetMethod.Name)
            {
                ReturnValue = new CsReturnValue(Ioc, null)
                {
                    PublicType = paramType,
                    MarshalType = paramType
                }
            };

            var iface = new CsInterface(null, null);
            iface.Add(getMethod);

            CsProperty prop = new(Ioc, null, "Active", getMethod, null);

            propertyBuilder.AttachPropertyToParent(prop);

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

            CsMethod setMethod = new(Ioc, cppSetMethod, cppSetMethod.Name)
            {
                ReturnValue = new CsReturnValue(Ioc, null)
                {
                    PublicType = TypeRegistry.Void
                }
            };

            setMethod.Add(new CsParameter(Ioc, null, null)
            {
                PublicType = paramType
            });

            var iface = new CsInterface(null, null);
            iface.Add(setMethod);

            CsProperty prop = new(Ioc, null, "Active", null, setMethod);

            propertyBuilder.AttachPropertyToParent(prop);

            Assert.Equal(Visibility.Internal, setMethod.Visibility);
        }

        [Fact]
        public void NonPropertyMethodWithNonPropertyNameShouldNotCreateProperty()
        {
            CsMethod setMethod = new(Ioc, null, "MyMethod")
            {
                ReturnValue = new CsReturnValue(Ioc, null)
                {
                    PublicType = TypeRegistry.Void
                }
            };

            Assert.Empty(propertyBuilder.CreateProperties(new[] { setMethod }));
        }
    }
}
