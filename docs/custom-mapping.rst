##################################
Customizing your SharpGen Mapping
##################################

Declaring C# types to SharpGen
===============================

SharpGen mantains an internal record of types it understands. If you want to reference a type it does not know about, then you have to declare that type to SharpGen. To declare a type to SharpGen, you use ``<define>`` or ``<create>`` elements under the ``<extension>`` element in the ``<config>`` tag. The following subsections show how to declare different types to SharpGen.

.. note::

    Any types defined with the ``<define>`` tag will **not** have any code generated for them. SharpGen assumes that any types defined with the ``<define>`` tag already exist, either in source or in a referenced assembly.

.. _groups:

Groups (Classes)
-----------------

Groups represent a type for SharpGen to place functions and constants and other C++ constructs that cannot be represented in the C# object oriented model. You need to declare at least one group if you want to map any non-member functions with SharpGen. To map a group, use the following syntax:

.. code-block:: xml

    <create class="MyNamespace.Functions" visibility="public static" />

The example above creates a ``static`` class named ``Functions`` in the ``MyNamespace`` namespace that is ``public``. You can attach functions (shown in :ref:`functions`)  and constants (shown in :ref:`constants`) to this class.

Interfaces/Classes
-------------------

Interfaces represent C++ classes or structures with at least one pure virtual method defined. They are generated as classes normally, but can be configured to generate as interfaces (so C# classses can implement them and be passed to native code) with more advanced mappings. These interfaces are called callbacks. You can find more information about callbacks in :doc:`/shadows`.

You will likely only define an interface if you are going to use it in a binding (see :ref:`typeBindings`). You can define an interface as shown below:

.. code-block:: xml

    <define interface="Qualified.Name.For.MyInterface" />

There are a few other attributes for interface definitions. These are listed below.

    * ``native``
        * The native implmentation of the interface.
    * ``shadow``
        * The name of the shadow type of the interface. Will be used if any derived interfaces are callbacks. See :doc:`/shadows` for more details.
    * ``vtbl``
        * The name of the vtbl type of the interface. Will be used if any derived interfaces are callbacks. See :doc:`/shadows` for more details. 

Structs
---------

Structs represent any structures in C++ that do not have any virtual functions. If a struct is hard for SharpGen to model or you already have accurate marshalling for it in source or a referenced assembly, you can use a ``<define>`` tag to define it. For the most efficient and accurate code generation, you should calculate the size of the structure in bytes. Below is a simple example of defining a struct.

.. code-block:: xml

    <define struct="Qualified.Name.For.MyStruct" sizeof="size_in_bytes" />

There are a few other attributes for struct defintions. These are listed below.

    * ``align``
        * The alignment of the structure. This generates as the ``Pack`` property on the ``StructLayout`` attribute.
    * ``marshal``
        * Whether or not this structure has a native marshal type. Because SharpGenTools uses the ``calli`` instruction, it has to handle native marshalling manually. If your type cannot be directly represented in unmanaged code from the managed instance, you must define marshalling code and set this attribute to ``true``. See :doc:`/native-marshalling` for documentation on how the SharpGenTools marshalling system works. 
    * ``static-marshal``
        * Whether or not the marshalling functions are defined as static functions or instance functions. More information can be found at :doc:`/native-marshalling`.
    * ``custom-new``
        * Whether or not there is a custom ``__NewNative`` method that should be used instead of just creating a new instance of the native structure. More information can be found at :doc:`/native-marshalling`.  

Enums
------

Enums represent a C++ enum. You would use a ``<define>`` tag for an enumeration that does not exist in code, but the parameter of a method can only take a finite subset of integer values. In that case, it would be helpful to your users to define a C# enum to ensure they only pass correct values. Below is an example for defining an enum:

.. code-block:: xml

    <define enum="Qualified.Name.For.MyEnum" underlying="System.UInt32" />

The ``underlying`` attribute defines what type this enumeration is pretending to be. It does not need to match the declared underlying type in C#.

Additionally, there you can specify the ``sizeof`` attribute instead of the ``underlying`` attribute if you prefer to specify the enumeration in terms of the size of its native representation. See the table below for what underlying type is picked for each value of ``sizeof``. If the ``sizeof`` value is not in the table, SharpGen will fail to generate code for your mapping.

================= =================
``sizeof`` value  Underlying Type
================= =================
1                 ``System.Byte``
2                 ``System.Int16``
4                 ``System.Int32``
================= =================

.. _typeBindings:

Defining Default Type Bindings
===============================

Sometimes the code for a specific type and the way it is used in the native code is hard for SharpGenTools to understand. In other cases, the type already exists in .NET and you want specific native types to always use it. You can use ``<bind>`` elements (in a ``<bindings>`` tag) to "bind" a native type to a managed type. An example of a bind element from SharpGen.Runtime is available below:

.. code-block:: xml

    <bind from="int" to="System.Int32" />

Additionally, if you want the type to be represented by one type for the user but marshalled to native with a different type, you can set the ``marshal`` attribute on the ``<bind>`` element as shown below:

.. code-block:: xml

    <bind from="bool" to="System.Boolean" marshal="System.Byte" />

In the example above, any element with type ``bool`` will be presented to users as a ``System.Boolean``, but will be marshalled to and from native code as a ``System.Byte``.

.. _functions:

Mapping Functions
===================

To map functions, you have to specify a group to which to attach the functions. Additionally, this group must be declared as shown in :ref:`groups`.  You can use a ``<map>`` element under a ``<mapping>`` tag in the ``<config>`` tag. You use the ``group`` attribute to specify which group to attach the functions to. For example:

.. code-block:: xml

    <map function="MyFunction" group="MyNamespace.Functions" />

Mapping Macros as Enums
========================

In some C++ libraries such as DirectX, a set of macros define the valid values for a parameter. SharpGenTools allows you to map these macros into C# as an enumeration. To do so, you use a ``<create-cpp>`` element in the ``<extension>`` element as shown below.

.. code-block:: xml

    <create-cpp macro="MY_MACRO_OPTIONS_.*" enum="MY_MACRO_OPTIONS" />

As you can see, you can use a regular expression in the ``macro`` attribute to select multiple macros to be members of this enumeration. This enumeration will be created during the parsing process and then mapped as a C++ enumeration with the default mapping rules.

.. _constants:

Macros and GUIDs as Constants
==============================

Constants are mapped with the ``<const>`` tag under the ``<extension>`` tag. You can map both macros and GUIDs to constant values in the generated code. All constants need to be attached to a group or other type. Set the ``class`` attribute to the name of the class or type to which the constant should be attached to in the generated code. Set the ``name`` attribute to the name of the generated member in the C# code.

Mapping GUIDs
----------------

To map a GUID, set the ``from-guid`` attribute on the ``<const>`` tag to the name of the GUID in the C++ code.

Mapping Macros
----------------

Mapping macros to constants is more interesting. Since C++ macros do not inherently have a type, you gain a lot of control for how the macro is mapped as a constant. Below is a list of the different attributes you use to map macros.

  * ``from-macro``

    * Which macro you are mapping to a constant.
  * ``type``

    * The C# type of the constant.
  * ``cpp-type``

    * The C++ type of the constant. This is optional if it is the same as the C# type.
  * ``cpp-cast``
  
    * The type to cast the macro value to in C++. This is only needed if an explicit cast is needed for the literal macro value to be assigned to the C++ type of the constant.

.. _constValueMap:

Constant Value Mapping
--------------------------

For the ``value`` attribute of the ``<const>`` tag, you can specify any (HTML-escaped) C# expression. The placeholders in the following table allow you to substitute in information about the constant.

============= ================================================================
Placeholder   Subsititution
============= ================================================================
$0            C++ name of the macro or GUID
$1            Value of the macro or GUID
$2            C# name of the macro or GUID converted to Pascal Case 
$3            Namespace declared in ``<namespace>``
============= ================================================================

.. note::

    The value of a GUID (for the placeholder $1) is the value of the result of a standard ``ToString()`` call on a ``System.Guid`` instance with the value of the mapped GUID.

Removing Elements
=====================

Sometimes you might want to map many elements in an include file, but not all of them. We supply the ``<remove>`` tag for you to remove items. Just set the attribute that matches the type of element you want to remove (from the table below) to a regular expression that matches all of the elements of that type you want to remove.

=========================== ===============
Element type to Remove      Attribute Name
=========================== ===============
Enum                         ``enum``
Enum Item                    ``enum-item``
Interface                    ``interface``
Method                       ``method``
Struct                       ``struct``
Field                        ``field``
Any other element            ``element``
Multiple types of elements   ``element``
=========================== ===============

.. warning::

    Removing some elements (such as parameters or struct fields) will likely cause invalid marshalling code-gen and may destabilize your application.
