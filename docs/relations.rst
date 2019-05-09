=================================================
SharpGen Mapping Relations
=================================================

Sometimes fields and parameters have values that are related to the containing structure (for fields), a constant value, or the length of another array field or parameter. Instead of either requiring your users to specify these values or requiring you to wrap the SharpGen-generated code in your own interface, SharpGen supports defining these relationships in your mapping files. SharpGen will then generate code that implements these relations so you do not have to wrap the code yourself. When the relation rules are applied, the field with the relation rule is hidden from the managed view of the structure, method, or function and is calculated when marshalling.

Struct Size Relation
====================

A common pattern in Windows APIs is to have a field, usually named ``cbSize`` that must be assigned the size of the structure. So, SharpGen provides a relation to automatically calculate this field for you. The following rule tells SharpGen that all fields named ``cbSize`` must be assigned the size of their containing structure.

.. code-block:: xml

    <map field=".*::cbSize" relation="struct-size()" />

All of the ``cbSize`` fields in a struct's native representation will be assigned the size of their containing (native) structure when marshalling the structure to its native representation.

This relationship is only valid on fields.

Constant Value Relation
=======================

Sometimes APIs have "reserved" fields or parameters, parameters that must have a specific value and are reserved for later use or undocumented. Since these fields and parameters will always have the same value, there is no value in requiring your consumer to supply these values. So, by using the constant-value relation, SharpGen will supply the reserved values itself when marshalling. For example, let's say that the function ``Foo`` takes a parameter named ``ireserved`` that is required to always have the value ``0``. The mapping rule below will instruct SharpGen to always assign the value ``0`` to the ``ireserved`` parameter.

.. code-block:: xml

    <map param="Foo::ireserved" relation="const(0)" />

This relation is valid on both fields and parameters.

.. _lengthRelation:

Length Relation
===============

When passing an array to native code, it is common to pass the length of the array as a separate parameter. Additonally, when generating a shadow implementation for an interface, SharpGen must know how long to construct the managed view of array parameters. To convey this information, SharpGen provides a relation for array length. For example, if a function ``Bar`` takes two parameters ``arr`` and ``len`` where ``len`` is the length of ``arr``, then the following rule will tell SharpGen that ``len`` is the length of ``arr``.

.. code-block:: xml

    <map param="Bar::len" relation="length(arr)" />

This relation not only automatically calculates the value of the ``len`` parameter when calling the native function, but when applied to a method, it allows SharpGen to correctly automatically generate a callback implementation when generating a shadow. Additionally, for managed to native calls, if a parameter has a native property named ``Length``, this relationship can be defined to set a parameter to have the value of that ``Length`` property.

This relation is valid on both fields and parameters.
