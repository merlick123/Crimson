set(CrimsonCommand "crimson" CACHE STRING "Command used to invoke Crimson")
set(CrimsonCommandArguments "" CACHE STRING "Extra arguments passed before the Crimson subcommand")

function(_crimson_cpp_run_build)
  get_filename_component(_crimson_project_root "${CMAKE_CURRENT_FUNCTION_LIST_DIR}/../.." ABSOLUTE)
  set(_crimson_project_file "${_crimson_project_root}/CppDeviceDemo.crimsonproj")
  separate_arguments(_crimson_command_arguments NATIVE_COMMAND "${CrimsonCommandArguments}")
  execute_process(
    COMMAND "${CrimsonCommand}" ${_crimson_command_arguments} build "${_crimson_project_file}"
    WORKING_DIRECTORY "${_crimson_project_root}"
    RESULT_VARIABLE _crimson_result
    OUTPUT_VARIABLE _crimson_stdout
    ERROR_VARIABLE _crimson_stderr)
  if(NOT _crimson_result EQUAL 0)
    message(FATAL_ERROR "Crimson build failed:\n${_crimson_stdout}${_crimson_stderr}")
  endif()
endfunction()

function(crimson_configure_cpp_cpp_target target_name)
  if(NOT TARGET "${target_name}")
    message(FATAL_ERROR "Target '${target_name}' does not exist.")
  endif()

  get_filename_component(_crimson_project_root "${CMAKE_CURRENT_FUNCTION_LIST_DIR}/../.." ABSOLUTE)
  set(_crimson_project_file "${_crimson_project_root}/CppDeviceDemo.crimsonproj")
  set_property(DIRECTORY APPEND PROPERTY CMAKE_CONFIGURE_DEPENDS "${_crimson_project_file}")
  file(GLOB_RECURSE _crimson_contract_files CONFIGURE_DEPENDS "${_crimson_project_root}/contracts/*.idl")
  separate_arguments(_crimson_command_arguments NATIVE_COMMAND "${CrimsonCommandArguments}")

  _crimson_cpp_run_build()

  set(_crimson_sources)
  file(GLOB_RECURSE _crimson_globbed_sources CONFIGURE_DEPENDS "${_crimson_project_root}/cpp/generated/src/*.cpp")
  list(APPEND _crimson_sources ${_crimson_globbed_sources})
  file(GLOB_RECURSE _crimson_globbed_sources CONFIGURE_DEPENDS "${_crimson_project_root}/cpp/user/src/*.cpp")
  list(APPEND _crimson_sources ${_crimson_globbed_sources})

  if(_crimson_sources)
    target_sources("${target_name}" PRIVATE ${_crimson_sources})
  endif()
  target_include_directories("${target_name}" PRIVATE "${_crimson_project_root}/cpp/generated/include")
  target_include_directories("${target_name}" PRIVATE "${_crimson_project_root}/cpp/user/include")

  add_custom_target("${target_name}_crimson_codegen"
    COMMAND "${CrimsonCommand}" ${_crimson_command_arguments} build "${_crimson_project_file}"
    WORKING_DIRECTORY "${_crimson_project_root}"
    DEPENDS ${_crimson_contract_files} "${_crimson_project_file}"
    VERBATIM)
  add_dependencies("${target_name}" "${target_name}_crimson_codegen")
endfunction()
