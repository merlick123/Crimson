cmake_minimum_required(VERSION 3.20)
project(CppDeviceDemoApp LANGUAGES CXX)

include("${CMAKE_CURRENT_SOURCE_DIR}/.crimson/cmake/Crimson.cpp.cmake")

add_executable(CppDeviceDemoApp
    app/main.cpp
)

target_compile_features(CppDeviceDemoApp PRIVATE cxx_std_20)
crimson_configure_cpp_cpp_target(CppDeviceDemoApp)
