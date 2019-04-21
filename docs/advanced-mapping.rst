=====================
Advanced Mappings
=====================

SharpGenTools allows you to heavily customize the mappings from C++ to C#. This page covers all of the currently supported mappings for each element type.

All attributes referred to below are on a ``<map>`` element under the ``<mapping>`` tag in the ``<config>`` tag.

.. warning::

    Mapping rules in ``<map>`` tags apply to all matching elements in the mapping, not just the elements from includes in the current config file. This allows users with multi-assembly mapping and config files (described in :doc:`/advanced-configuration`) to reuse mapping rules without having to include unneeded files in the reused config. You can limit what elements are matched to specific include files with context rules, explained in :ref:`context`.


Selecting Elements
===================

Each mapping rule has to select elements to modify. You select elements by using a specific attribute on the ``<map>`` tag (see the :ref:`table below<mapTagSelect>`), along with a C# regular expression as the value. Do note, the start and end matchers ``^`` and ``$`` are implied, so you do not need to add them to your regular expression.

To select an element, your regular expression needs to match its C++ qualified name. For enumeration items, you use the name that does not include the name of the enumeration. For methods, parameters, and fields, you use the qualified name of the parent concatenated with ``::`` and then the name of the element. For purposes of a "parent", the method is the parent of the parameter.

Additionally, you can prepend a ``#`` to your selector to select the immediate parent of the matched element. This is useful in mapping some COM libraries where an enumeration member is well known, but the enum name is a non-user-friendly generated name.

.. _mapTagSelect:

===================== ===================== ===============================
Mapping Tag Attribute Elements Modified     Link to Specific Mapping Rules
===================== ===================== ===============================
``enum``              Enumerations          :ref:`enum` 
``enum-item``         Enumeration Items     :ref:`enumitem` 
``struct``            Structs               :ref:`struct` 
``field``             Struct Fields         :ref:`field` 
``interface``         Interfaces            :ref:`interface` 
``method``            Interface Methods     :ref:`callable` 
``function``          Non-member Functions  :ref:`callable` 
``param``             Parameters            :ref:`parameter` 
``element``           All matching elements All sections applicable
``doc``               Documentation Links   :ref:`docLink` 
===================== ===================== ===============================

.. _context:

Context Directives
===================

Context directives allow you to limit which includes mapping rules apply to. You can do this via the ``<context>`` tag.

To add a context for a set of mapping rules, add a ``<context>IncludeName</context>`` before the rules, and a ``<context-clear />`` after the rules. You can add multiple ``<context>`` tags to enable rules to affect a multiple headers and only those headers.

If you have multiple headers you want to repeatedly use as a singular context, you can define a context set such as below:

.. code-block:: xml

    <context-set id="common-context">
        <context>firstHeader</context>
        <context>secondHeader</context>
    </context-set>

You can then enable this context with the context tag as follows ``<context>common-context</context>``.

If you want to limit rules to applying to C++ elements that were generated from a macro or GUID, you need to add two context tags as shown below:

.. code-block:: xml

    <context>myConfigId</context>
    <context>myConfigId-ext</context>


.. warning::
    
    Additionally, the name in the ``<context>`` tag must match case with the first time the header was included, even if the header was first included transitively via a different header. If they don't match, the mapping rules will not affect the correct context.

.. _general-rules:

General
==========

    * ``name-tmp``

        * Rename the element to the name given here and allow naming rules (in :doc:`/naming-rules`) to still run on these elements. 
        * The value here can be a C# regex replacement expression for the corresponding regex used in the selector.
    * ``name``

        * Rename the element to the name given here and do not allow naming rules (in :doc:`/naming-rules`) to still run on these elements. 
        * The value here can be a C# regex replacement expression for the corresponding regex used in the selector.

    .. _visibility:

    * ``visibility``

        * Override the visibility and other modifiers of the resulting C# element. Options are listed below.
        * public
        * internal
        * protected
        * private
        * override
        * abstract
        * partial
        * static
        * const
        * virtual
        * readonly
        * sealed
    * ``naming``

        * Override default naming rules. Options are listed below and explained in more detail in :doc:`/naming-rules` 
        * ``default``

            * Use default naming rules.
        * ``noexpand``
        
            * Do not expand short name rules for the name of this element.
        * ``underscore``

            * Keep the underscores between each part of the original name if it was ``snake_case``.


.. _type-rules:

All Types
=========

    * ``assembly``

        * The assembly the type should be in. When specified, this requires a ``namespace`` attribute as well.
    * ``namespace``

        * The namespace the type should be in. When specified, this requires an ``assembly`` attribute as well.
    
.. _marshallable:

All Marshallable Elements
=========================

    * ``type``

        * The type presented to the user in the mapping.
    * ``override-native-type``
    
        * Override the native representation with the type specified in the ``type`` attribute.
    * ``pointer``

        * Override the pointer arity of the matching C++ elements.
    * ``array``

        * Override the array dimension of the matching C++ elements.
    * ``relation``

        * Specify that this marshallable element is related to another marshallable element or has a constant value. See :doc:`/relations` for more information.

.. _enum:

Enums
=======

    * ``flags``

        * Specifies if this enumeration should be generated with the ``[Flags]`` attribute.
    * ``none``

        * Specifies if SharpGenTools should generate a member named None with the value 0.

.. _enumitem:

Enum Items
===========

Enum items can only be configured with the :ref:`general-rules` rules.

.. _struct:

Structs
==========

    * ``native``

        * Force generation of native marshal type for this struct.
    * ``struct-to-class``

        * Generate this structure as a C# ``class`` instead of a ``struct``.
    * ``marshal``

        * Marks this struct as having custom marshalling methods, so marshalling methods will not be generated. See :doc:`/native-marshalling` for details. 
    * ``static-marshal``

        * Marks that this struct uses static marshalling methods. See :doc:`/native-marshalling` for details. 
    * ``new``

        * Marks that this struct's native structure has a custom construction method instead of the constructor. See :doc:`/native-marshalling` for details. 
    * ``marshalto``

        * Forces generation of the ``__MarshalTo`` method. See :doc:`/native-marshalling` for details. 
    * ``pack``

        * Specifies the packing/alignment of the structure.

.. _field:

Fields
========

Fields can only be configured with the :ref:`general-rules` and :ref:`marshallable` rules. 

.. _interface:

Interfaces
=============

    * ``callback``

        * Generate this interface as a callback interface. See :doc:`/shadows` for details. 
    * ``callback-dual``

        * Generate this interface as a callback interface, but also generate a default implementation for C++ instances of the interface. See :doc:`/shadows` for details. 
    * ``callback-visibility``

        * The visibility for the default implementation. See the :ref:`visibility options<visibility>` for possible values. 
    * ``callback-name``

        * A custom name for the default implementation. Defaults to the original name + "Native".
    * ``autogen-shadow``

        * Automatically generate the shadow classes for this callback interface. See :doc:`/shadows` for details.
    * ``shadow-name``

        * A custom name for the shadow type. See :doc:`/shadows` for details.
    * ``vtbl-name``

        * A custom name for the vtbl type. See :doc:`/shadows` for details.

.. _callable:

Functions and Methods
======================

    * ``check``

        * Enable or disable automatically checking the error code. Defaults to true (enabled).
    * ``hresult``

        * Force the error code to be returned.
    * ``rawptr``

        * Force generation of a private overload of the method that has all array, pointer, or interface parameters as ``IntPtr``.
    * ``return``

        * Always return the return value of the function.
    * ``type``
        * The user-visible return type of the element.
    
Rules for methods only:

    * ``property``

        * Enable or disable automatic property generation. Defaults to true (enabled).
    * ``persist``

        * Cache the value for the generated property getter the first time the getter is called.
    * ``custom-vtbl``

        * Generate a private variable for the virtual method table index of the method, so it can be customized at runtime as needed.
    * ``offset-translate``

        * Offset the virtual method table index by a this value.
    * ``keep-implement-public``

        * If the parent interface has ``dual-callback`` set to ``true``, then keep the implementation of this method in the default implementation public.

Rules for functions only:

    * ``dll``
        
        * The expression to put in the ``DllImport`` attribute as the dll name.
    * ``group``
    
        * The group (see :ref:`groups`) to attach the function to. 

.. _parameter:

Parameters
=============

    * ``attribute``

        * Override the attributes for this parameter. Options are below:
        * ``none`` 
        * ``in`` 

            * This parameter is an input parameter.
        * ``out`` 

            * This parameter is an output parameter.
        * ``inout`` 

            * This parameter is both an in and out parameter;
        * ``buffer``

            * This parameter takes in an array of elements.
        * ``optional`` 

            * This parameter is optional.
        * ``fast`` 

            * If this parameter is an out parameter, reuse the C# interface instance for the returned value by setting the ``NativePointer`` property.
        * ``params`` 

            * Use the C# params modifier on this parameter.
        * ``value`` 
            
            * Force a C# value type to be passed by value to the generated method for this parameter even if the size is greater than 16 bytes.

    * ``return``

        * Use this parameter as the return value of the generated C# method/function for this parameter's parent C++ method/function.

Miscellaneous
==============

.. _docLink:

Doc Links
----------

    * ``name``

        * The element that all doc references to the selected elements should reference in the generated documentation.