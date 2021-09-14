/*
  Copyright Kitware, Inc.

  Licensed under the Apache License, Version 2.0 (the "License");
  you may not use this file except in compliance with the License.
  You may obtain a copy of the License at

      https://www.apache.org/licenses/LICENSE-2.0

  Unless required by applicable law or agreed to in writing, software
  distributed under the License is distributed on an "AS IS" BASIS,
  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  See the License for the specific language governing permissions and
  limitations under the License.
*/

/* cl -c -FoNUL detect_vs.c */
#define TO_STRING0(x) #x
#define TO_STRING(x) TO_STRING0(x)
#define TO_DEFINE(x) "#define " #x " " TO_STRING(x)

#pragma message("")
#ifdef __ATOM__
# pragma message(TO_DEFINE(__ATOM__))
#endif
#ifdef __AVX__
# pragma message(TO_DEFINE(__AVX__))
#endif
#ifdef __AVX2__
# pragma message(TO_DEFINE(__AVX2__))
#endif
#ifdef _ATL_VER
# pragma message(TO_DEFINE(_ATL_VER))
#endif
#ifdef _CHAR_UNSIGNED
# pragma message(TO_DEFINE(_CHAR_UNSIGNED))
#endif
#ifdef _CONTROL_FLOW_GUARD
# pragma message(TO_DEFINE(_CONTROL_FLOW_GUARD))
#endif
#ifdef _CPPRTTI
# pragma message(TO_DEFINE(_CPPRTTI))
#endif
#ifdef _CPPUNWIND
# pragma message(TO_DEFINE(_CPPUNWIND))
#endif
#ifdef _DEBUG
# pragma message(TO_DEFINE(_DEBUG))
#endif
#ifdef _DLL
# pragma message(TO_DEFINE(_DLL))
#endif
#ifdef _INTEGRAL_MAX_BITS
# pragma message(TO_DEFINE(_INTEGRAL_MAX_BITS))
#endif
#ifdef _ISO_VOLATILE
# pragma message(TO_DEFINE(_ISO_VOLATILE))
#endif
#ifdef _KERNEL_MODE
# pragma message(TO_DEFINE(_KERNEL_MODE))
#endif
#ifdef _MANAGED
# pragma message(TO_DEFINE(_MANAGED))
#endif
#ifdef _MFC_VER
# pragma message(TO_DEFINE(_MFC_VER))
#endif
#ifdef _MSC_BUILD
# pragma message(TO_DEFINE(_MSC_BUILD))
#endif
#ifdef _MSC_EXTENSIONS
# pragma message(TO_DEFINE(_MSC_EXTENSIONS))
#endif
#ifdef _MSC_FULL_VER
# pragma message(TO_DEFINE(_MSC_FULL_VER))
#endif
#ifdef _MSC_VER
# pragma message(TO_DEFINE(_MSC_VER))
#endif
#ifdef _MT
# pragma message(TO_DEFINE(_MT))
#endif
#ifdef _M_ALPHA
# pragma message(TO_DEFINE(_M_ALPHA))
#endif
#ifdef _M_AMD64
# pragma message(TO_DEFINE(_M_AMD64))
#endif
#ifdef _M_ARM
# pragma message(TO_DEFINE(_M_ARM))
#endif
#ifdef _M_ARM64
# pragma message(TO_DEFINE(_M_ARM64))
#endif
#ifdef _M_ARM_ARMV7VE
# pragma message(TO_DEFINE(_M_ARM_ARMV7VE))
#endif
#ifdef _M_ARM_FP
# pragma message(TO_DEFINE(_M_ARM_FP))
#endif
#ifdef _M_CEE
# pragma message(TO_DEFINE(_M_CEE))
#endif
#ifdef _M_CEE_PURE
# pragma message(TO_DEFINE(_M_CEE_PURE))
#endif
#ifdef _M_CEE_SAFE
# pragma message(TO_DEFINE(_M_CEE_SAFE))
#endif
#ifdef _M_FP_EXCEPT
# pragma message(TO_DEFINE(_M_FP_EXCEPT))
#endif
#ifdef _M_FP_FAST
# pragma message(TO_DEFINE(_M_FP_FAST))
#endif
#ifdef _M_FP_PRECISE
# pragma message(TO_DEFINE(_M_FP_PRECISE))
#endif
#ifdef _M_FP_STRICT
# pragma message(TO_DEFINE(_M_FP_STRICT))
#endif
#ifdef _M_IA64
# pragma message(TO_DEFINE(_M_IA64))
#endif
#ifdef _M_IX86
# pragma message(TO_DEFINE(_M_IX86))
#endif
#ifdef _M_IX86_FP
# pragma message(TO_DEFINE(_M_IX86_FP))
#endif
#ifdef _M_MPPC
# pragma message(TO_DEFINE(_M_MPPC))
#endif
#ifdef _M_MRX000
# pragma message(TO_DEFINE(_M_MRX000))
#endif
#ifdef _M_PPC
# pragma message(TO_DEFINE(_M_PPC))
#endif
#ifdef _M_X64
# pragma message(TO_DEFINE(_M_X64))
#endif
#ifdef _NATIVE_WCHAR_T_DEFINED
# pragma message(TO_DEFINE(_NATIVE_WCHAR_T_DEFINED))
#endif
#ifdef _OPENMP
# pragma message(TO_DEFINE(_OPENMP))
#endif
#ifdef _PREFAST_
# pragma message(TO_DEFINE(_PREFAST_))
#endif
#ifdef _VC_NODEFAULTLIB
# pragma message(TO_DEFINE(_VC_NODEFAULTLIB))
#endif
#ifdef _WCHAR_T_DEFINED
# pragma message(TO_DEFINE(_WCHAR_T_DEFINED))
#endif
#ifdef _WIN32
# pragma message(TO_DEFINE(_WIN32))
#endif
#ifdef _WIN64
# pragma message(TO_DEFINE(_WIN64))
#endif
#ifdef _WINRT_DLL
# pragma message(TO_DEFINE(_WINRT_DLL))
#endif
#ifdef _Wp64
# pragma message(TO_DEFINE(_Wp64))
#endif
#ifdef __CLR_VER
# pragma message(TO_DEFINE(__CLR_VER))
#endif
#ifdef __MSVC_RUNTIME_CHECKS
# pragma message(TO_DEFINE(__MSVC_RUNTIME_CHECKS))
#endif
#ifdef __STDC__
# pragma message(TO_DEFINE(__STDC__))
#endif
#ifdef __STDC_HOSTED__
# pragma message(TO_DEFINE(__STDC_HOSTED__))
#endif
