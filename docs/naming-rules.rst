========================
SharpGen Naming Rules
========================

Default Naming Rules
=======================

SharpGen has a few default name mappings that it applies to C++ elements when creating the C# model. Below is an ordering of how SharpGenTools maps names:

    1. If this C++ element has a name specified from a mapping rule, use that name.
    2. If the mapping rule is not set using ``name-tmp``, stop here.
    3. If the name is not in ``snake_case``, not in all caps, and the first letter is capitalized, stop here.
    4. (Enum Items only) If the item name starts with the name of the enumeration, remove the enumeration name.
    5. Remove the leading underscore if one exists.
    6. Apply custom rules (explained in :ref:`customNaming`) if the naming flags aren't set to ``noexpand``.
    7. Convert to Pascal case.
    8. (Pointer parameters only) If the name originally started with ``pp``, append ``Out`` to the final name and remove the ``pp`` prefix. If the name originally started with just ``p``, append ``Ref`` to the final name and remove the ``p`` prefix.
    9. (Parameters only) If the final name starts with a digit, prepend ``arg`` to the name.
    10. (Parameters only) Convert to ``camelCase``.

.. _customNaming:

Adding Naming Rules
==========================

In addition to the direct name mappings and the default naming rules defined above, there are extension points in configuration files for custom naming rules. All of these rules go under the ``<naming>`` tag under the ``<config>`` tag.

"Short" Rule
----------------

"Short" rules are basic regex text substitution rules. You can use them to expand domain specific acronyms. You can add a short rule like below:

.. code-block:: xml

    <short name="CLR">CommonLanguageRuntime</short>
