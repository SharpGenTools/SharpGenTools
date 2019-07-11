#################################
SharpGenTools SDK Documentation
#################################

What is SharpGenTools
=======================

SharpGenTools is a code generator that generates C# for interoperation with C++ and COM libraries. It generates efficient C# code and handles all of the native marshalling. Additionally, it includes an assembly patcher to use CLR instructions not available from C# to further minimize the overhead of the generated code.

Why SharpGenTools?
====================

SharpGenTools makes it extremely easy for your C#-C++ interop to have "one source of truth", the C++ header files. Since SharpGenTools generates the C# headers on build by default, it helps ensure that they stay up to date with the matching C++ header files. It also directly integrates into MSBuild, so the user has do to minimal work to integrate it into their project. Also, all of the structures and interfaces are generated as ``partial`` structures/classes/interfaces, so you have the ability to fully customize them to your needs.

SharpGenTools vs CppSharp
-------------------------

Pros for SharpGenTools:

* Configuration file based

   * Does not require the user to build a driver program to generate C# code.
   * Generated code extremely configurable
   * Escape hatches to allow the user to manually write mappings SharpGenTools cannot handle.
* Full MSBuild integration
* Consumer file passthough

   * Consuming projects that also generate C# code will automatically gain knowledge about type mappings and defined types from project references and directly referenced packages.
* More efficient method calls on virtual methods of native instances (``calli`` instruction vs. ``Marshal.GetDelegateForFunctionPointer``).

Pros for CppSharp:

* More supported C++ features (operator overloading)
* Better support for custom passes, so may allow more configurability than SharpGenTools.

Getting started
===================
Check out :doc:`getting-started` to get started!.


.. toctree::
   :maxdepth: 2
   :caption: Contents
   
   release-notes
   getting-started
   custom-mapping
   advanced-mapping
   relations
   config-features
   naming-rules
   documentation
   native-marshalling
   shadows
   platform-detection
   advanced-configuration
   limitations