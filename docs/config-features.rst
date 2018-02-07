#####################################
SharpGenTools Config File Features
#####################################

The SharpGenTools config files have a few nice features to ease mapping development.

Macros
========

You can define SharpGen macros in your project file as below:

.. code-block:: xml

    <PropertyGroup>
        <SharpGenMacros>$(SharpGenMacros);MY_MACRO</SharpGenMacros>
    </PropertyGroup>

Then, within your config file, you can surround **any** XML tag(s) with an ``<ifdef name="MY_MACRO">`` tag. Then, when ``MY_MACRO`` is defined, those rules/includes will be included, but otherwise they will be ignored.

Variables
==========

Variables can be defined in your mapping file to give you some shorthand to use common expressions. You can define a variable with the ``<var>`` tag as shown:

.. code-block:: xml

    <var name="varname" value="value" />

You can refer to this variable in any attribute value or element value as ``$(varname)``. The variable ``$(THIS_CONFIG_PATH)`` is to the directory path the file is in.

Dynamic Variables
===================

Dynamic Variables allow your mapping rules to use the values of C++ macros in attributes or element values of mapping and binding rules. A dynamic variable is defined for every C++ macro SharpGenTools parsed while parsing the C++ headers. To use a dynamic variable, you can use the syntax ``#(DYNAMIC_VARIABLE_NAME)``. SharpGen will give you an error if the dynamic variable cannot be found.