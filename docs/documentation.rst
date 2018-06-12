============================================
Making SharpGenTools Document Your Mappings
============================================

SharpGenTools gives you two options for how to automatically document your mappings.

External Documentation Comments
================================

You can supply external documentation though XML files structured as below:

.. code-block:: xml

    <comments>
        <comment id="C++ or C# Element Name">
            <summary>Summary here</summary>
        </comment>
    </comments>

SharpGenTools will automatically include an ``<include>`` documentation tag that points to a matching element in an external documentation file. You can specify an external comments file to SharpGen by adding it as a ``SharpGenExternalDocs`` item in your project file.

Doc Providers
==================

Sometimes you're mapping an already documented library, and you don't want to have to manually extract the documentation for each element you're documenting. SharpGen provides the ``IDocProvider`` interface that you can extend:

.. include:: ../SharpGen/Doc/IDocProvider.cs
    :start-line: 19
    :code: csharp

You can reference the SharpGen package on NuGet to get a reference to the assembly. To enable installed doc providers, set the ``$(SharpGenGenerateDoc)`` property to ``true``. By default, SharpGenTools.Sdk does not ship with any doc providers.

Doc Providers MSBuild Integration
=====================================

To integrate into MSBuild, you need to create an MSBuild task. The MSDN Doc Provider task project in the SharpGen.Doc.Msdn repository (SharpGen.Doc.Msdn.Tasks), is a great example of how to create your own doc provider and hook it into the build process. If you are going to publicly publish the doc provider task, please make the task share the same condition statement as the one in SharpGen.Doc.Msdn.Tasks so users can easily enable the provider in a standard way.