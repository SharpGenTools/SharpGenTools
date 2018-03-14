cmake_minimum_required (VERSION 3.0)
set(LIB_TYPE SHARED)

set(STRUCT_SOURCES Struct/*.cpp)
add_library(StructNative ${LIB_TYPE} ${STRUCT_SOURCES})

set(INTERFACE_SOURCES Interface/*.cpp)
add_library(InterfaceNative ${LIB_TYPE} ${INTERFACE_SOURCES})