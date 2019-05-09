######################################
SharpGen Callbacks and Shadows
######################################

SharpGenTools has support as part of the SharpGen.Runtime package for inheriting from interfaces generated from C++ and passing C# implementations to native code. In the mapping files, the ``callback`` mapping rule specifies SharpGen should generate an interface instead of a class for the C++ interface.

Auto-Generated Shadows
=======================

To auto-generate the shadow for a callback, add the ``autogen-shadow="true"`` attribute to a mapping rule for the callback interface. This will automatically generate a shadow interface corresponding with the instructions below using the same marshalling code-gen infrastructure as the rest of the generated code.

If any parameters of any methods on the interface are arrays, you must provide an array length relation in your mapping rules. Otherwise your code will be generated with an unconditional ``InvalidOperationException``. See :ref:`lengthRelation` for more information.

.. warning::

    Property generation does not currently play nice with the shadow auto-generator. You may need to use a mapping rule to disable property generation. See :ref:`callable` for details on the mapping rule. 

Manually-Written Shadows
===========================

To declare the shadow type for a callback, put the ``SharpGen.Runtime.ShadowAttribute`` attribute on the interface type. The parameter is a ``typeof(MyInterfaceShadow)`` expression where the type is the shadow type. For an example, look at the code in ``ComStreamBaseShadow.cs`` in the ``SharpGen.Runtime.COM`` folder.

.. note::

    SharpGen does not currently generate method signatures for callback interfaces if it is not auto-generating shadows. You will have to specify the method signatures in another file defining the interface.


Shadow Objects
---------------

The shadow object is the object that connects the native object with the .NET object instance. Most of your shadow classes will be similar to the shadow class below:

.. code-block:: csharp

    private class MyInterfaceShadow : BaseInterfaceShadow // Use CppObjectShadow if there isn't a base interface
    {
        private static MyInterfaceVtbl Vtbl = new MyInterfaceVtbl(0);

        protected override CppObjectVtbl GetVtbl => Vtbl;
    }

Most of the interesting work happens in the Vtbl type, which we cover in the next section.


Virtual Method Table (Vtbl) Type
---------------------------------

The Vtbl type builds the virtual method table and has methods to marshal calls from native back into the managed code. The Vtbl class should be declared similar to as follows within the Shadow type:

.. code-block:: csharp

    protected class MyInterfaceVtbl : BaseInterfaceVtbl // Use CppObjectVtbl if there isn't a base interface
    {
    }

Vtbl Constructor
----------------

The Vtbl constructor should look similar to below:

.. code-block:: csharp

    public MyInterfaceVtbl(int numberOfMethods)
        :base(numberOfMethods + numMethodsInMyInterface)
    {

    }

The ``numberOfMethods`` parameter allows derived classes to also be callbacks. The base vtbl class requires the number of methods on the interface to correctly allocate for the native vtbl.

There are statements we need to put into the constructor, but we will cover those in the next section.


Vtbl Methods
-------------

Each method in the interface has to have a corresponding vtbl method and delegate. The delegate has to have an ``[UnmanagedFunctionPointer]`` attribute that specifies the native calling convention. Additionally, the delegate must have the first parameter be an ``IntPtr`` to represent the ``this`` pointer, and the remaining parameters must be directly representable directly in native code. To add it into the vtbl, add a statement in the constructor in the order that it was declared in native code:

.. code-block:: csharp

    AddMethod(new MyMethodDelegate(MyMethod));

The declaration of the method must be ``static`` and have the same parameters and return value as the delegate.

Guidelines for Vtbl Method implementations
----------------------------------------------

Here are a few guidelines for vtbl method implementations:

    * To get the shadow object for the ``this`` pointer, call ``ToShadow<MyInterfaceShadow>(thisPtr)``.
    * To get the .NET object from the shadow, cast the ``Callback`` property on the shadow object to the interface type.
    * Wrap the call to the .NET object in a ``try``-``catch`` so .NET exceptions do not escape into native code. I make no promises that escaping exceptions won't crash the application.
