using SharpGen.Config;
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

            var returnType = new CsFundamentalType(typeof(int));

            var isMethod = new CsMethod
            {
                Name = "IsActive",
                ReturnValue = new CsReturnValue
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

            var returnType = new CsFundamentalType(typeof(int));

            var getMethod = new CsMethod
            {
                Name = "GetActive",
                ReturnValue = new CsReturnValue
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

            var paramType = new CsFundamentalType(typeof(int));

            var setMethod = new CsMethod
            {
                Name = "SetActive",
                ReturnValue = new CsReturnValue
                {
                    PublicType = new CsFundamentalType(typeof(void))
                }
            };

            setMethod.Add(new CsParameter
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

            var paramType = new CsFundamentalType(typeof(int));

            var setMethod = new CsMethod
            {
                Name = "SetActive",
                ReturnValue = new CsReturnValue
                {
                    PublicType = new CsStruct { Name = "SharpGen.Runtime.Result" }
                }
            };

            setMethod.Add(new CsParameter
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

            var paramType = new CsFundamentalType(typeof(int));

            var getMethod = new CsMethod
            {
                Name = "GetActive",
                ReturnValue = new CsReturnValue
                {
                    PublicType = new CsStruct
                    {
                        Name = "SharpGen.Runtime.Result"
                    }
                }
            };

            getMethod.Add(new CsParameter
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

            var returnType = new CsFundamentalType(typeof(int));

            var getMethod = new CsMethod
            {
                Name = "GetActive",
                ReturnValue = new CsReturnValue
                {
                    PublicType = returnType,
                    MarshalType = returnType
                },
            };

            var invalidSetMethod = new CsMethod
            {
                Name = "SetActive",
                ReturnValue = new CsReturnValue
                {
                    PublicType = new CsFundamentalType(typeof(void))
                }
            };

            Assert.Empty(propertyBuilder.CreateProperties(new[] { getMethod, invalidSetMethod }));

            Assert.Empty(propertyBuilder.CreateProperties(new[] { invalidSetMethod, getMethod }));
        }

        [Fact]
        public void InvalidGetterDoesNotCreateProperty()
        {
            var propertyBuilder = new PropertyBuilder(new GlobalNamespaceProvider());

            var returnType = new CsFundamentalType(typeof(void));

            var getMethod = new CsMethod
            {
                Name = "GetActive",
                ReturnValue = new CsReturnValue
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

            var paramType = new CsFundamentalType(typeof(int));

            var setMethod = new CsMethod
            {
                Name = "SetActive",
                ReturnValue = new CsReturnValue
                {
                    PublicType = new CsFundamentalType(typeof(void))
                }
            };

            Assert.Empty(propertyBuilder.CreateProperties(new[] { setMethod }));
        }

        [Fact]
        public void GeneratePropertyIfGetterAndSetterMatch()
        {
            var propertyBuilder = new PropertyBuilder(new GlobalNamespaceProvider());

            var paramType = new CsFundamentalType(typeof(int));

            var getMethod = new CsMethod
            {
                Name = "GetActive",
                ReturnValue = new CsReturnValue
                {
                    PublicType = paramType,
                    MarshalType = paramType
                },
            };

            var setMethod = new CsMethod
            {
                Name = "SetActive",
                ReturnValue = new CsReturnValue
                {
                    PublicType = new CsFundamentalType(typeof(void))
                }
            };

            setMethod.Add(new CsParameter
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

            var paramType = new CsFundamentalType(typeof(int));

            var getMethod = new CsMethod
            {
                Name = "GetActive",
                ReturnValue = new CsReturnValue
                {
                    PublicType = paramType,
                    MarshalType = paramType
                },
            };

            var setMethod = new CsMethod
            {
                Name = "SetActive",
                ReturnValue = new CsReturnValue
                {
                    PublicType = new CsFundamentalType(typeof(void))
                }
            };

            setMethod.Add(new CsParameter
            {
                PublicType = new CsFundamentalType(typeof(short))
            });

            var props = propertyBuilder.CreateProperties(new[] { getMethod, setMethod });
            Assert.Empty(props);
        }

        [Fact]
        public void DoesNotGeneratePropertyIfGetterAndSetterMismatch_ParameterizedGetter()
        {
            var propertyBuilder = new PropertyBuilder(new GlobalNamespaceProvider());

            var paramType = new CsFundamentalType(typeof(int));

            var getMethod = new CsMethod
            {
                Name = "GetActive",
                ReturnValue = new CsReturnValue
                {
                    PublicType = new CsStruct
                    {
                        Name = "SharpGen.Runtime.Result"
                    }
                }
            };

            getMethod.Add(new CsParameter
            {
                PublicType = paramType,
                Attribute = CsParameterAttribute.Out
            });

            var setMethod = new CsMethod
            {
                Name = "SetActive",
                ReturnValue = new CsReturnValue
                {
                    PublicType = new CsFundamentalType(typeof(void))
                }
            };

            setMethod.Add(new CsParameter
            {
                PublicType = new CsFundamentalType(typeof(short))
            });

            var props = propertyBuilder.CreateProperties(new[] { getMethod, setMethod });
            Assert.Empty(props);
        }

        [Fact]
        public void DoesNotGeneratePropertyIfOverloaded()
        {
            var propertyBuilder = new PropertyBuilder(new GlobalNamespaceProvider());

            var paramType = new CsFundamentalType(typeof(int));

            var getMethod = new CsMethod
            {
                Name = "GetActive",
                ReturnValue = new CsReturnValue
                {
                    PublicType = paramType,
                    MarshalType = paramType
                },
            };

            var getMethodOverload = new CsMethod
            {
                Name = "GetActive",
                ReturnValue = new CsReturnValue
                {
                    PublicType = paramType,
                    MarshalType = paramType
                },
            };

            var setMethod = new CsMethod
            {
                Name = "SetActive",
                ReturnValue = new CsReturnValue
                {
                    PublicType = new CsFundamentalType(typeof(void))
                }
            };

            setMethod.Add(new CsParameter
            {
                PublicType = paramType
            });

            Assert.Empty(propertyBuilder.CreateProperties(new[] { getMethod, setMethod, getMethodOverload }));
            Assert.Empty(propertyBuilder.CreateProperties(new[] { getMethod, getMethodOverload, setMethod }));
        }

        [Fact]
        public void PropertyAttachedToGetterType()
        {
            var propertyBuilder = new PropertyBuilder(new GlobalNamespaceProvider());

            var paramType = new CsFundamentalType(typeof(int));

            var getMethod = new CsMethod
            {
                Name = "GetActive",
                ReturnValue = new CsReturnValue
                {
                    PublicType = paramType,
                    MarshalType = paramType
                },
                AllowProperty = true
            };

            var iface = new CsInterface();
            iface.Add(getMethod);

            var prop = new CsProperty("Active")
            {
                Getter = getMethod
            };

            propertyBuilder.AttachPropertyToParent(prop);

            Assert.Equal(iface, prop.Parent);
        }

        [Fact]
        public void SetOnlyPropertyAttachedToSetterType()
        {
            var propertyBuilder = new PropertyBuilder(new GlobalNamespaceProvider());

            var paramType = new CsFundamentalType(typeof(int));

            var setMethod = new CsMethod
            {
                Name = "SetActive",
                ReturnValue = new CsReturnValue
                {
                    PublicType = new CsFundamentalType(typeof(void))
                },
                AllowProperty = true
            };

            setMethod.Add(new CsParameter
            {
                PublicType = paramType
            });

            var iface = new CsInterface();
            iface.Add(setMethod);

            var prop = new CsProperty("Active")
            {
                Setter = setMethod
            };

            propertyBuilder.AttachPropertyToParent(prop);

            Assert.Equal(iface, prop.Parent);
        }

        [Fact]
        public void PropertyNotAttachedWhenGetterAllowPropertyIsFalse()
        {
            var propertyBuilder = new PropertyBuilder(new GlobalNamespaceProvider());

            var paramType = new CsFundamentalType(typeof(int));

            var getMethod = new CsMethod
            {
                Name = "GetActive",
                ReturnValue = new CsReturnValue
                {
                    PublicType = paramType,
                    MarshalType = paramType
                },
                AllowProperty = false
            };

            var iface = new CsInterface();
            iface.Add(getMethod);

            var prop = new CsProperty("Active")
            {
                Getter = getMethod
            };

            propertyBuilder.AttachPropertyToParent(prop);

            Assert.Null(prop.Parent);
        }

        [Fact]
        public void PropertyNotAttachedWhenSetterAllowPropertyIsFalse()
        {
            var propertyBuilder = new PropertyBuilder(new GlobalNamespaceProvider());

            var paramType = new CsFundamentalType(typeof(int));

            var setMethod = new CsMethod
            {
                Name = "SetActive",
                ReturnValue = new CsReturnValue
                {
                    PublicType = new CsFundamentalType(typeof(void))
                },
                AllowProperty = false
            };

            setMethod.Add(new CsParameter
            {
                PublicType = paramType
            });

            var iface = new CsInterface();
            iface.Add(setMethod);

            var prop = new CsProperty("Active")
            {
                Setter = setMethod
            };

            propertyBuilder.AttachPropertyToParent(prop);

            Assert.Null(prop.Parent);
        }

        [Fact]
        public void PersistentGetterGeneratesPersistentProperty()
        {
            var propertyBuilder = new PropertyBuilder(new GlobalNamespaceProvider());

            var paramType = new CsFundamentalType(typeof(int));

            var getMethod = new CsMethod
            {
                Name = "GetActive",
                ReturnValue = new CsReturnValue
                {
                    PublicType = paramType,
                    MarshalType = paramType
                },
                AllowProperty = true,
                IsPersistent = true
            };

            var iface = new CsInterface();
            iface.Add(getMethod);

            var prop = new CsProperty("Active")
            {
                Getter = getMethod
            };

            propertyBuilder.AttachPropertyToParent(prop);

            Assert.True(prop.IsPersistent);
        }

        [Fact]
        public void GetterVisibiltyInternal()
        {
            var propertyBuilder = new PropertyBuilder(new GlobalNamespaceProvider());

            var paramType = new CsFundamentalType(typeof(int));

            var getMethod = new CsMethod
            {
                Name = "GetActive",
                ReturnValue = new CsReturnValue
                {
                    PublicType = paramType,
                    MarshalType = paramType
                },
                AllowProperty = true
            };

            var iface = new CsInterface();
            iface.Add(getMethod);

            var prop = new CsProperty("Active")
            {
                Getter = getMethod
            };

            propertyBuilder.AttachPropertyToParent(prop);

            Assert.Equal(Visibility.Internal, getMethod.Visibility);
        }

        [Fact]
        public void SetterVisibilityInternal()
        {
            var propertyBuilder = new PropertyBuilder(new GlobalNamespaceProvider());

            var paramType = new CsFundamentalType(typeof(int));

            var setMethod = new CsMethod
            {
                Name = "SetActive",
                ReturnValue = new CsReturnValue
                {
                    PublicType = new CsFundamentalType(typeof(void))
                },
                AllowProperty = true
            };

            setMethod.Add(new CsParameter
            {
                PublicType = paramType
            });

            var iface = new CsInterface();
            iface.Add(setMethod);

            var prop = new CsProperty("Active")
            {
                Setter = setMethod
            };

            propertyBuilder.AttachPropertyToParent(prop);

            Assert.Equal(Visibility.Internal, setMethod.Visibility);
        }

        [Fact]
        public void NonPropertyMethodWithNonPropertyNameShouldNotCreateProperty()
        {
            var propertyBuilder = new PropertyBuilder(new GlobalNamespaceProvider());

            var paramType = new CsFundamentalType(typeof(int));

            var setMethod = new CsMethod
            {
                Name = "MyMethod",
                ReturnValue = new CsReturnValue
                {
                    PublicType = new CsFundamentalType(typeof(void))
                }
            };

            Assert.Empty(propertyBuilder.CreateProperties(new[] { setMethod }));
        }
    }
}
