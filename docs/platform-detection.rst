####################################
SharpGen Runtime Platform Detection
####################################

Various platforms have differences in their native calling conventions or in the sizes of native types. To support these platforms, SharpGenTools supports transparently generating code that correctly handles multiple platforms in a single set of generated code. In SharpGenTools version 2.x and newer, the default-generated code will support all supported platforms if the code requires it.

Selecting Specific Platforms
=============================

Sometimes the native library that you are mapping is only available on a specific platform. For example, the DirectX suite is Windows-only. For these platforms, there is no reason to have code generated to support non-Windows platforms. You can request that your code is only generated to support specific platforms by adding the following elements to your project file:

.. code-block:: xml

    <ItemGroup>
        <SharpGenPlatforms Include="Windows" /> <!-- Enable code-gen for Windows platforms -->
        <SharpGenPlatforms Include="ItaniumSystemV" /> <!-- Enable code-gen for non-Windows platforms implementing the Itanium and SystemV ABIs (Linux, macOS, and others) -->
    </ItemGroup>

Selecting Specific Platforms with RIDs
========================================

In addition to using ``<SharpGenPlatforms />`` items, SharpGenTools supports inferring which platforms to generate code for by the ``<RuntimeIdentifier />`` property. As a result, self-contained .NET Core deployments or RID-specific framework dependent appliations will automatically only generate code for the specific platform they are compiling for.
