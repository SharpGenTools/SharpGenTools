using System.Linq;
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Model;
using SharpGen.Transform;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.UnitTests.Mapping
{
    public class Interface : MappingTestBase
    {
        public Interface(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public void Simple()
        {
            var config = new ConfigFile
            {
                Id = nameof(Simple),
                Namespace = nameof(Simple),
                Includes =
                {
                    new IncludeRule
                    {
                        File = "interface.h",
                        Attach = true,
                        Namespace = nameof(Simple)
                    }
                },
                Bindings =
                {
                    new BindRule("int", "System.Int32")
                }
            };

            var iface = new CppInterface("Interface")
            {
                TotalMethodCount = 1
            };

            iface.Add(new CppMethod("method")
            {
                ReturnValue = new CppReturnValue
                {
                    TypeName = "int"
                }
            });

            var include = new CppInclude("interface");

            include.Add(iface);

            var module = new CppModule("SharpGenTestModule");
            module.Add(include);

            var (solution, _) = MapModel(module, config);

            Assert.Single(solution.EnumerateDescendants<CsInterface>());

            var csIface = solution.EnumerateDescendants<CsInterface>().First();

            Assert.Single(csIface.Methods);

            var method = csIface.Methods.First();

            Assert.Equal(0, method.Offset);

            Assert.IsType<CsFundamentalType>(method.ReturnValue.PublicType);

            Assert.Equal(TypeRegistry.Int32, method.ReturnValue.PublicType);
        }

        [Fact]
        public void DualCallbackFlowsNativeImplementation()
        {
            var config = new ConfigFile
            {
                Id = nameof(Simple),
                Namespace = nameof(Simple),
                Includes =
                {
                    new IncludeRule
                    {
                        File = "interface.h",
                        Attach = true,
                        Namespace = nameof(Simple)
                    }
                },
                Bindings =
                {
                    new BindRule("int", "System.Int32")
                },
                Mappings =
                {
                    new MappingRule
                    {
                        Interface = "Interface",
                        IsCallbackInterface = true,
                        IsDualCallbackInterface = true
                    }
                }
            };

            var iface = new CppInterface("Interface")
            {
                TotalMethodCount = 1
            };

            iface.Add(new CppMethod("method")
            {
                ReturnValue = new CppReturnValue
                {
                    TypeName = "int"
                }
            });

            var include = new CppInclude("interface");

            include.Add(iface);

            var module = new CppModule("SharpGenTestModule");
            module.Add(include);

            var (_, defines) = GetConsumerBindings(module, config);

            var interfaceDefine = defines.First(define => define.Interface == "Simple.Interface");

            Assert.Equal("Simple.InterfaceNative", interfaceDefine.NativeImplementation);
        }

        [Fact]
        public void DefineWithNativeImplementationDefinesNativeImplementationType()
        {
            var config = new ConfigFile
            {
                Id = nameof(DefineWithNativeImplementationDefinesNativeImplementationType),
                Namespace = nameof(DefineWithNativeImplementationDefinesNativeImplementationType),
                Includes =
                {
                    new IncludeRule
                    {
                        File = "interface.h",
                        Attach = true,
                        Namespace = nameof(DefineWithNativeImplementationDefinesNativeImplementationType)
                    }
                },
                Extension =
                {
                    new DefineExtensionRule
                    {
                        Interface = "Imported.Param",
                        NativeImplementation = "Imported.ParamNative"
                    }
                },
                Bindings =
                {
                    new BindRule("int", "System.Int32"),
                    new BindRule("Param", "Imported.Param")
                }
            };

            var iface = new CppInterface("Interface")
            {
                TotalMethodCount = 1
            };

            var method = new CppMethod("method")
            {
                ReturnValue = new CppReturnValue
                {
                    TypeName = "int"
                }
            };

            method.Add(new CppParameter("param")
            {
                TypeName = "Param",
                Pointer = "*"
            });

            iface.Add(method);

            var include = new CppInclude("interface");

            include.Add(iface);

            var module = new CppModule("SharpGenTestModule");
            module.Add(include);

            var (solution, _) = MapModel(module, config);

            Assert.Single(solution.EnumerateDescendants<CsParameter>());

            var param = solution.EnumerateDescendants<CsParameter>().First();

            Assert.IsType<CsInterface>(param.PublicType);

            Assert.NotNull(((CsInterface)param.PublicType).NativeImplementation);

            Assert.False(Logger.HasErrors);
        }

        [Fact]
        public void ReturnStructByPointer()
        {
            ConfigFile config = new()
            {
                Id = nameof(ReturnStructByPointer),
                Namespace = nameof(ReturnStructByPointer),
                Includes =
                {
                    new IncludeRule
                    {
                        File = "d3d12.h",
                        Attach = true,
                        Namespace = nameof(ReturnStructByPointer)
                    }
                },
                Bindings =
                {
                    new BindRule("int", "System.Int32"),
                    new BindRule("DESC", "SharpGen.Runtime.PointerSize"),
                    new BindRule("UINT", "System.UInt32")
                }
            };

            CppStruct rootSig = new("D3D12_ROOT_SIGNATURE_DESC")
            {
                Items = new[]
                {
                    new CppField("NumParameters")
                    {
                        TypeName = "UINT",
                        Offset = 0
                    },
                    new CppField("pParameters")
                    {
                        TypeName = "DESC",
                        Pointer = "*",
                        Const = true,
                        Offset = 4
                    },
                    new CppField("NumStaticSamplers")
                    {
                        TypeName = "UINT",
                        Offset = 8
                    },
                    new CppField("pStaticSamplers")
                    {
                        TypeName = "DESC",
                        Pointer = "*",
                        Const = true,
                        Offset = 12
                    },
                    new CppField("Flags")
                    {
                        TypeName = "UINT",
                        Offset = 16
                    }
                }
            };

            CppInterface iface = new("ID3D12RootSignatureDeserializer")
            {
                TotalMethodCount = 1,
                Items = new[]
                {
                    new CppMethod("GetRootSignatureDesc")
                    {
                        ReturnValue = new CppReturnValue
                        {
                            Const = true,
                            TypeName = rootSig.FullName,
                            Pointer = "*"
                        }
                    }
                }
            };

            CppModule module = new("SharpGenTestModule")
            {
                Items = new[]
                {
                    new CppInclude("d3d12")
                    {
                        Items = new CppContainer[]
                        {
                            rootSig,
                            iface
                        }
                    }
                }
            };

            var (solution, _) = MapModel(module, config);

            Assert.Empty(solution.EnumerateDescendants<CsParameter>());
            Assert.Single(solution.EnumerateDescendants<CsReturnValue>());

            var returnValue = solution.EnumerateDescendants<CsReturnValue>().Single();
            Assert.True(returnValue.HasPointer);
            Assert.Equal(rootSig.Name, returnValue.PublicType.CppElementName);

            Assert.False(Logger.HasErrors);
        }

        [Fact]
        public void VoidInBufferParameter()
        {
            ConfigFile config = new()
            {
                Id = nameof(VoidInBufferParameter),
                Namespace = nameof(VoidInBufferParameter),
                Includes =
                {
                    new IncludeRule
                    {
                        File = "dxcapi.h",
                        Attach = true,
                        Namespace = nameof(VoidInBufferParameter)
                    }
                },
                Extension =
                {
                    new DefineExtensionRule
                    {
                        Struct = "SharpGen.Runtime.Result",
                        SizeOf = 4,
                        IsNativePrimitive = true
                    },
                    new DefineExtensionRule
                    {
                        Struct = "SharpGen.Runtime.PointerSize",
                        SizeOf = 8,
                        IsNativePrimitive = true
                    }
                },
                Bindings =
                {
                    new BindRule("HRESULT", "SharpGen.Runtime.Result"),
                    new BindRule("SIZE_T", "SharpGen.Runtime.PointerSize"),
                    new BindRule("UINT32", "System.UInt32"),
                    new BindRule("void", "System.Void")
                }
            };

            CppInterface blob = new("IDxcBlob")
            {
                Guid = "8BA5FB08-5195-40e2-AC58-0D989C3A0102",
                Items = new[]
                {
                    new CppMethod("GetBufferPointer")
                    {
                        ReturnValue = new CppReturnValue
                        {
                            TypeName = "void",
                            Pointer = "*"
                        }
                    },
                    new CppMethod("GetBufferSize")
                    {
                        ReturnValue = new CppReturnValue
                        {
                            TypeName = "SIZE_T"
                        }
                    }
                }
            };

            CppInterface blobEncoding = new("IDxcBlobEncoding")
            {
                Guid = "7241d424-2646-4191-97c0-98e96e42fc68",
                Base = blob.Name
            };

            CppInterface iface = new("IDxcUtils")
            {
                TotalMethodCount = 1,
                Items = new[]
                {
                    new CppMethod("CreateBlobFromPinned")
                    {
                        CallingConvention = CppCallingConvention.StdCall,
                        ReturnValue = new CppReturnValue
                        {
                            TypeName = "HRESULT"
                        },
                        Items = new[]
                        {
                            new CppParameter("pData")
                            {
                                Const = true,
                                TypeName = "void",
                                Pointer = "*",
                                Attribute = ParamAttribute.In | ParamAttribute.Buffer
                            },
                            new CppParameter("size")
                            {
                                TypeName = "UINT32",
                                Attribute = ParamAttribute.In
                            },
                            new CppParameter("codePage")
                            {
                                TypeName = "UINT32",
                                Attribute = ParamAttribute.In
                            },
                            new CppParameter("pBlobEncoding")
                            {
                                TypeName = "IDxcBlobEncoding",
                                Pointer = "**",
                                Attribute = ParamAttribute.Out
                            }
                        }
                    }
                }
            };

            CppModule module = new("SharpGenTestModule")
            {
                Items = new[]
                {
                    new CppInclude("dxcapi")
                    {
                        Items = new CppContainer[]
                        {
                            blob,
                            blobEncoding,
                            iface
                        }
                    }
                }
            };

            var (solution, _) = MapModel(module, config);

            var csBlob = solution.EnumerateDescendants<CsInterface>(false)
                                 .SingleOrDefault(x => x.Name == blob.Name);
            var csBlobEncoding = solution.EnumerateDescendants<CsInterface>(false)
                                         .SingleOrDefault(x => x.Name == blobEncoding.Name);
            var csUtils = solution.EnumerateDescendants<CsInterface>(false)
                                  .SingleOrDefault(x => x.Name == iface.Name);

            Assert.NotNull(csBlob);
            Assert.NotNull(csBlobEncoding);
            Assert.NotNull(csUtils);

            Assert.Equal(blob.Guid, csBlob.Guid);
            Assert.Equal(blobEncoding.Guid, csBlobEncoding.Guid);

            var method = csUtils.EnumerateDescendants<CsMethod>().SingleOrDefault();

            Assert.NotNull(method);
            Assert.Equal(CppCallingConvention.StdCall, method.CppCallingConvention);

            var methodParams = method.EnumerateDescendants<CsParameter>().ToArray();

            Assert.Equal(4, methodParams.Length);

            Assert.Equal("data", methodParams[0].Name);
            Assert.Equal("size", methodParams[1].Name);
            Assert.Equal("codePage", methodParams[2].Name);
            Assert.Equal("blobEncoding", methodParams[3].Name);

            Assert.False(methodParams[0].IsOut);
            Assert.False(methodParams[1].IsOut);
            Assert.False(methodParams[2].IsOut);
            Assert.True(methodParams[3].IsOut);
            Assert.True(methodParams[0].IsIn);
            Assert.True(methodParams[1].IsIn);
            Assert.True(methodParams[2].IsIn);
            Assert.False(methodParams[3].IsIn);
            Assert.Equal(TypeRegistry.IntPtr, methodParams[0].MarshalType);
            Assert.Equal(TypeRegistry.IntPtr, methodParams[0].PublicType);
            Assert.Equal(TypeRegistry.UInt32, methodParams[1].MarshalType);
            Assert.Equal(TypeRegistry.UInt32, methodParams[1].PublicType);
            Assert.Equal(TypeRegistry.UInt32, methodParams[2].MarshalType);
            Assert.Equal(TypeRegistry.UInt32, methodParams[2].PublicType);
            Assert.Equal(csBlobEncoding, methodParams[3].MarshalType);
            Assert.Equal(csBlobEncoding, methodParams[3].PublicType);
            Assert.True(methodParams[0].HasPointer);
            Assert.False(methodParams[0].IsArray);
            Assert.False(methodParams[0].IsInterface);
            Assert.True(methodParams[0].IsValueType);
            Assert.True(methodParams[0].IsPrimitive);
            Assert.False(methodParams[0].HasParams);
            Assert.False(methodParams[1].HasPointer);
            Assert.False(methodParams[1].IsArray);
            Assert.False(methodParams[1].IsInterface);
            Assert.True(methodParams[1].IsValueType);
            Assert.True(methodParams[1].IsPrimitive);
            Assert.False(methodParams[1].HasParams);
            Assert.False(methodParams[2].HasPointer);
            Assert.False(methodParams[2].IsArray);
            Assert.False(methodParams[2].IsInterface);
            Assert.True(methodParams[2].IsValueType);
            Assert.True(methodParams[2].IsPrimitive);
            Assert.False(methodParams[2].HasParams);
            Assert.True(methodParams[3].HasPointer);
            Assert.False(methodParams[3].IsArray);
            Assert.True(methodParams[3].IsInterface);
            Assert.False(methodParams[3].IsValueType);
            Assert.False(methodParams[3].IsPrimitive);
            Assert.False(methodParams[3].HasParams);

            Assert.False(Logger.HasErrors);
        }

        [Fact]
        public void BoundIntPtrInParameter()
        {
            ConfigFile config = new()
            {
                Id = nameof(BoundIntPtrInParameter),
                Namespace = nameof(BoundIntPtrInParameter),
                Includes =
                {
                    new IncludeRule
                    {
                        File = "d3d9.h",
                        Attach = true,
                        Namespace = nameof(BoundIntPtrInParameter)
                    }
                },
                Extension =
                {
                    new DefineExtensionRule
                    {
                        Struct = "SharpGen.Runtime.Result",
                        SizeOf = 4,
                        IsNativePrimitive = true
                    }
                },
                Bindings =
                {
                    new BindRule("HRESULT", "SharpGen.Runtime.Result"),
                    new BindRule("HDC", "System.IntPtr")
                }
            };

            CppInterface iface = new("IDirect3DSurface9")
            {
                TotalMethodCount = 1,
                Items = new[]
                {
                    new CppMethod("ReleaseDC")
                    {
                        CallingConvention = CppCallingConvention.StdCall,
                        ReturnValue = new CppReturnValue
                        {
                            TypeName = "HRESULT"
                        },
                        Items = new[]
                        {
                            new CppParameter("hdc")
                            {
                                TypeName = "HDC",
                                Attribute = ParamAttribute.In
                            },
                        }
                    }
                }
            };

            CppModule module = new("SharpGenTestModule")
            {
                Items = new[]
                {
                    new CppInclude("d3d9")
                    {
                        Items = new CppContainer[]
                        {
                            iface
                        }
                    }
                }
            };

            var (solution, _) = MapModel(module, config);

            var csUtils = solution.EnumerateDescendants<CsInterface>(false)
                                  .SingleOrDefault(x => x.Name == iface.Name);

            Assert.NotNull(csUtils);

            var method = csUtils.EnumerateDescendants<CsMethod>().SingleOrDefault();

            Assert.NotNull(method);
            Assert.Equal(CppCallingConvention.StdCall, method.CppCallingConvention);

            var methodParams = method.EnumerateDescendants<CsParameter>().ToArray();

            Assert.Single(methodParams);

            var param = methodParams[0];
            Assert.Equal("hdc", param.Name);

            Assert.False(param.IsOut);
            Assert.True(param.IsIn);
            Assert.Equal(TypeRegistry.IntPtr, param.MarshalType);
            Assert.Equal(TypeRegistry.IntPtr, param.PublicType);
            Assert.False(param.HasPointer);
            Assert.False(param.IsArray);
            Assert.False(param.IsInterface);
            Assert.True(param.IsValueType);
            Assert.True(param.IsPrimitive);
            Assert.False(param.HasParams);

            var interopParams = method.InteropSignatures.ToArray();
            Assert.Single(interopParams);
            Assert.Equal(PlatformDetectionType.Any, interopParams[0].Key);

            var interopParam = interopParams[0].Value;
            Assert.Equal("int", interopParam.ReturnType);
            Assert.Single(interopParam.ParameterTypes);
            Assert.Equal($"_{param.Name}", interopParam.ParameterTypes[0].Name);
            Assert.Equal(TypeRegistry.VoidPtr, interopParam.ParameterTypes[0].InteropType);

            Assert.False(Logger.HasErrors);
        }
    }
}
