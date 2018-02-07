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

You can reference the SharpGen package on NuGet to get a reference to the assembly. Once you've built an assembly with your Doc Provider, set the ``SharpGenGenerateDoc`` property to ``true`` and the ``SharpGenDocProviderAssemblyPath`` property to the path to the assembly.

By default, SharpGen has a built in MSDN Doc Provider that fetches documentation from the MSDN documentation service. To use this, just set ``SharpGenGenerateDoc`` to ``true``.