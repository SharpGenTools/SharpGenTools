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

General
==========

.. _enum:

Enums
=======

.. _enumitem:

Enum Items
===========

.. _struct:

Structs
==========

.. _field:

Fields
========

.. _interface:

Interfaces
=============

.. _callable:

Functions and Methods
======================

.. _parameter:

Parameters
=============

.. _marshallable:

All Marshallable Elements
=========================

Miscellaneous
==============

.. _docLink:

Doc Links
----------
