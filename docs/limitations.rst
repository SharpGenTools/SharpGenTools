#######################
Mapping Limitations
#######################

Below are a list of limitations in SharpGen's current generator. Many of these are on the roadmap for future versions.

    * Does not map virtual but not pure virtual member functions.
    * Does not map classes with virtual functions and state.
    * Does not call constructors of C++ classes.
    * Non-member functions must be declared ``extern "C"`` for the ``DllImport`` entry point to find the function.
    * Does not map state for classes with pure virtual members.
    * Does not map non-virtual member functions.
    * Does not map function pointer parameters to C# delegate types.

