#################################
SharpGenTools SDK Documentation
#################################

.. toctree::
   :maxdepth: 2
   :caption: Contents
   
   getting-started
   config-spec

What is SharpGenTools
=======================

SharpGenTools is a code generator that generates C# for interoperation with C++ and COM libraries. It generates effecient C# code and handles all of the native marshalling. Additionally, it includes an assembly patcher to use CLR instructions not available from C# to further minimize the overhead of the generated code.

Why SharpGenTools?
====================

SharpGenTools makes it extremely easy for your C#-C++ interop to have "one source of truth", the C++ header files. Since SharpGenTools generates the C# headers on build by default, it helps ensure that they stay up to date with the matching C++ header files. It also directly integrates into MSBuild, so the user has do to minimal work to integrate it into their project.

SharpGenTools vs CppSharp
-------------------------

Pros for SharpGenTools:

* Configuration file based

   * Does not require the user to build a driver program to generate C# code.
   * Generated code extremely configurable
* Full MSBuild integration
* Consumer file passthough

   * Consuming projects that also generate C# code will automatically gain knowledge about type mappings and defined types from project references and directly referenced packages.
* More efficient method calls on virtual methods of native instances (``calli`` instruction vs. ``Marshal.GetDelegateForFunctionPointer``).

Pros for CppSharp:

* More supported C++ features (operator overloading)
* Better support for custom passes, so may allow more configurability than SharpGenTools.

Installing SharpGenTools
========================

To use SharpGenTools, you'll want to install the SDK package, as well as the Runtime package. The SDK package provides MSBuild tooling, and the runtime package provides the required support classes for the code generated from the SDK. You can use the following commands from the .NET CLI to install them:

::

   dotnet add package SharpGenTools.Sdk
   dotnet add package SharpGen.Runtime

.. note::

   These packages are separate so advanced and legacy users, specifically the SharpDX project, can use their own runtime support classes. In nearly all cases, you will want to reference both projects.
