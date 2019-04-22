=================================================
Advanced SharpGenTools Configuration Options
=================================================

SharpGenTools gives you many different configuration options in MSBuild. They're listed below:

    * ``SharpGenGenerateDoc``
    
        * Generate documentation from a doc provider (see :doc:`/documentation`)
        * Defaults to ``false``
    * ``SharpGenGenerateConsumerBindMapping``

        * Generate a bind mapping file that allows consumers of your project that use SharpGenTools to extend your mappings without having to copy yours.
        * Defaults to ``true``
    * ``SharpGenGlobalNamespace``

        * Use the given namespace as the namespace for the runtime support types SharpGen uses.
        * Defaults to ``SharpGen.Runtime``

    * ``SharpGenIncludeAssemblyNameFolder``

        * Include the name of the assembly as a directory in the output path.
        * Defaults to ``false``
    * ``SharpGenDocumentationOutputDir``

        * The output directory for the documentation cache.
    * ``SharpGenGeneratedCodeFolder``

        * The name of the folder in the output path to place the generated code in.
        * Defaults to ``$(IntermediateOutputPath)SharpGen/Generated``
    * ``SharpGenOutputDirectory``

        * The base directory for code output.
        * Defaults to ``$(MSBuildProjectDirectory)``

Generated Code Output Directory Structure
=============================================

Some of the options above control the output directory for generated code. The two options below are the different ways the output path is constructed.

    * If ``SharpGenIncludeAssemblyNameFolder`` is false, ``$(SharpGenOutputDirectory)/$(SharpGenGeneratedCodeFolder)``
    * If ``SharpGenIncludeAssemblyNameFolder`` is true, ``$(SharpGenOutputDirectory)/SharpGenAssemblyName/$(SharpGenGeneratedCodeFolder)``


CastXML Customization
=========================

    * ``CastXmlPath``

        * A path to a custom build of CastXML.
    * ``CastXmlArg`` MSBuild Items

        * Additional arguments to pass to CastXML when parsing the C++ code.