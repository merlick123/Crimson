cmake_minimum_required(VERSION 3.20)
project(SmartHomeCppApp LANGUAGES CXX)

include("${CMAKE_CURRENT_SOURCE_DIR}/.crimson/cmake/Crimson.cpp.cmake")

add_executable(SmartHomeCppApp
    cpp-app/main.cpp
)

target_compile_features(SmartHomeCppApp PRIVATE cxx_std_20)
crimson_configure_cpp_cpp_target(SmartHomeCppApp)
