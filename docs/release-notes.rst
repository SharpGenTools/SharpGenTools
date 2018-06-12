=====================
Release Notes
=====================

1.vNext
==========

This release focuses on improving codegen of already supported features and autogenerating some code that previously required manual generation.

Features:

    * Flow information about callbacks to consuming projects.
    * Opt-in autogeneration of shadows for callback interfaces.
    * Miscellanous bug fixes.

Development Changes:

    * Building now requires CMake and doesn't require the Windows SDK.
    * The COM support library is now in a separate repository.
    * Debug build now collects code coverage, enabling verification that our code works in our automated tests.
    * MSDN Documentation provider and tasks in a separate repository.

1.0.0
==========

This is the first stable release of SharpGenTools. Below is a summary of features available in SharpGenTools.

    * Support for mapping C++ "interfaces"
    * Support for generating C++ interface callbacks so C++ interfaces can be implemented in C#.
    * Support for mapping C++ structures and auto-generating marshal types.
    * Support for mapping C++ functions with C linkage.
    * Custom doc providers supported.
    * MSDN Doc provider in separate assembly.
    * External documentation files support.
    * Runtime support via SharpGen.Runtime package.
    * Pre-mapped COM support via SharpGen.Runtime.COM package.
    * Package dependency mapping flow - Include prologs, defines, type bindings, and doc links flow to consumers.
    * Support for re-patching signed assemblies.
    * Automatically locate the Visual C++ includes folder when including the standard library on Windows.