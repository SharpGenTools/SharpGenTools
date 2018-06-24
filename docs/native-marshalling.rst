#############################
SharpGen Native Marshalling
#############################

Because SharpGen uses ``calli`` instructions instead of ``Marshal.GetDelegateForFunctionPointer`` delegates, it bypasses all of the .NET runtime's built in marshalling code. As a result, SharpGen has to ensure that each type it passes to and from native code has the same bit-level representation as the native representation of the type.

Default Marshalling
=====================

By default, SharpGen only generates a native marshal type for types that have a different managed representation. The different conditions that trigger generation of a native marshal struct are listed below. If any condition is true for at least one field, a marshal struct is generated.

Array fields
-------------

Fields with fixed length arrays require a native marshal struct since the managed representation cannot lay out an array member in memory without forcing all consumers to use ``unsafe``, which does not make a good user experience.

String fields
-------------------------

String fields are always marshalled across as a ``System.IntPtr``. As a result, any string fields force generation of a marshal structure.

Interface fields
------------------------

Interface fields are mashalled across as a ``System.IntPtr`` but presented in the user structure as an instance of the interface type.

Overridden User-facing Type
----------------------------

If the user-facing type is overriden without overriding the native type (via the ``override-native-type`` attribute on a mapping rule), then we need to generate a marshal struct to correctly marshal between the native representation and the managed structure.

Bind Marshal Type
------------------

If a type is bound with a ``<bind>`` rule and the ``<bind>`` rule has a ``marshal`` attribute that differs from the ``to`` attribute, then a marshal struct will be generated.

Struct fields with Marshal Structs
-----------------------------------

As should be obvious, if a member of this struct has a struct type that needs a native marshal struct, then we have to generate a native marshal struct for this struct as well.

Special Case: Enums
--------------------

Enum types have the same bit-level representation as their underlying type, so we don't need to generate a marshal struct.

Special Case: Bool fields backed by Integer types
--------------------------------------------------

If an integer field is mapped to a bool, we can do all conversions efficiently with just the regular struct type while keeping the native representation of this field the same. So, for these fields, called "bool to int" fields, we don't need to generate a native marshal structure.

Marshalling Member Names
=========================

Here's a list of the names and signatures of members that SharpGen generates for marshalling. All of these members are by default ``internal`` instance members on the structure.

    * ``struct __Native``

        * The structure with the native representation of the struct.
    * ``void __MarshalFrom(ref __Native @ref)``

        * Marshals from the native struct to the managed struct.
    * ``void __MarshalFree(ref __Native @ref)``

        * Free any unmanaged resources, such as memory, that was allocated for the native structure.
    * ``void __MarshalTo(ref __Native @ref)``

        * Marshal data from the managed structure to the native structure. Only generated if needed.

Marshalling Extension Points
=============================

Custom Marshal Functions
--------------------------

When custom marshalling is enabled through a mapping rule, SharpGen will generate code assuming that the structure has native marshalling, but you as the consumer must manually write the marshalling methods. You use this setting when SharpGen can't correctly generate the native structure and the marshalling.

Static Marshal Functions
-------------------------

When static marshalling is enabled through a mapping rule, SharpGen will generate marshalling code in the structure, but methods will reference static marshalling methods with the following signatures that you must supply:

    * ``static void __MarshalFrom(ref StructName @this, ref __Native @ref)``
    * ``static void __MarshalFree(ref __Native @ref)``
    * ``static void __MarshalTo(ref StructName @this, ref __Native @ref)``

Custom Native New Function
---------------------------

Sometimes you might need to specially initialize some of the members of your structure when constructing it. As a result, SharpGenTools allows you to specify a custom function to create new instances of the native instance that will be used when constructing single instances (non-arrays) of the native structure. The signature you must supply is below:

.. code-block:: csharp

    static __Native __NewNative();
