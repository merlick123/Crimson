namespace Crimson.Core.Projects;

public sealed class CppCMakeCrossProjectInitProfile : IProjectInitProfile
{
    public string ProfileId => "cpp-cmake-cross";

    public string DisplayName => "C++ / CMake / Cross";

    public string Description => "C++ output with CMake integration and a generic cross-toolchain scaffold.";

    public ProjectInitPlan CreatePlan(ProjectInitContext context)
    {
        var files = new List<ProjectInitFile>
        {
            new("README.md", CppCMakeProjectInitProfile.RenderReadme(context.ProjectName, "cpp-cmake-cross", "cmake --preset cross-debug", "cmake --build --preset cross-debug", "The preset points at the generated generic toolchain scaffold and the CMake module runs `crimson build` automatically.")),
            new("CMakeLists.txt", CppCMakeProjectInitProfile.RenderCMakeLists(context.ProjectName, "cpp")),
            new("CMakePresets.json", CMakePresets),
            new(Path.Combine("cmake", "toolchains", "generic.cmake"), ToolchainFile),
            new(Path.Combine("app", "main.cpp"), context.Starter ? CppCMakeProjectInitProfile.StarterMain : CppCMakeProjectInitProfile.DefaultMain),
        };

        if (context.Starter)
        {
            files.Add(new ProjectInitFile(Path.Combine("contracts", "hello.idl"), CppCMakeProjectInitProfile.StarterIdl));
        }

        return new ProjectInitPlan(
            [
                new ProjectInitGroup(
                    "cpp",
                    "cpp",
                    ["contracts/**/*.idl"],
                    Array.Empty<string>(),
                    "cpp",
                    new { },
                    new ProjectInitHost("cmake", new { buildDirectory = "build" }))
            ],
            files);
    }

    private const string CMakePresets = """
{
  "version": 3,
  "configurePresets": [
    {
      "name": "cross-debug",
      "generator": "Unix Makefiles",
      "binaryDir": "${sourceDir}/build/cross-debug",
      "cacheVariables": {
        "CMAKE_BUILD_TYPE": "Debug",
        "CMAKE_TOOLCHAIN_FILE": "${sourceDir}/cmake/toolchains/generic.cmake"
      }
    }
  ],
  "buildPresets": [
    {
      "name": "cross-debug",
      "configurePreset": "cross-debug"
    }
  ]
}
""";

    private const string ToolchainFile = """
# Generic cross-toolchain scaffold for Crimson C++ projects.
# Copy this file or replace the variables below to match your target platform.

set(CMAKE_SYSTEM_NAME Generic CACHE STRING "")
set(CMAKE_SYSTEM_PROCESSOR unknown CACHE STRING "")

# Point these at your cross toolchain as needed.
# set(CMAKE_C_COMPILER clang CACHE FILEPATH "")
# set(CMAKE_CXX_COMPILER clang++ CACHE FILEPATH "")
# set(CMAKE_ASM_COMPILER clang CACHE FILEPATH "")

# If your environment provides a sysroot or SDK, configure it here.
# set(CMAKE_SYSROOT /path/to/sysroot CACHE PATH "")

# If your platform requires custom linker flags or platform libraries, add them here.
# set(CMAKE_EXE_LINKER_FLAGS_INIT "" CACHE STRING "")
""";
}
