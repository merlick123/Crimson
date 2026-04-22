namespace Crimson.Core.Projects;

public sealed class CppCMakeProjectInitProfile : IProjectInitProfile
{
    public string ProfileId => "cpp-cmake";

    public string DisplayName => "C++ / CMake";

    public string Description => "C++ output with generated/user source split and CMake integration.";

    public ProjectInitPlan CreatePlan(ProjectInitContext context)
    {
        var files = new List<ProjectInitFile>
        {
            new("README.md", RenderReadme(context.ProjectName, "cpp-cmake", "cmake -S . -B build", "cmake --build build", "The generated CMake module runs `crimson build` automatically during configure and build.")),
            new("CMakeLists.txt", RenderCMakeLists(context.ProjectName)),
            new(Path.Combine("app", "main.cpp"), context.Starter ? StarterMain : DefaultMain),
        };

        if (context.Starter)
        {
            files.Add(new ProjectInitFile(Path.Combine("contracts", "hello.idl"), StarterIdl));
        }

        return new ProjectInitPlan(
            ["contracts/**/*.idl"],
            Array.Empty<string>(),
            [new ProjectInitTarget("cpp", new { output = "cpp" })],
            new ProjectInitHost("cmake", new { buildDirectory = "build" }),
            files);
    }

    internal static string RenderReadme(
        string projectName,
        string profileId,
        string configureCommand,
        string buildCommand,
        string workflowNote) => $$"""
# {{projectName}}

This project uses Crimson with the `{{profileId}}` init profile.

Configure and build it from this directory:

```bash
{{configureCommand}}
{{buildCommand}}
```

{{workflowNote}}

Override the Crimson command if you are using a local repo build:

```bash
{{configureCommand}} \
  -DCrimsonCommand=dotnet \
  -DCrimsonCommandArguments="run --project /path/to/src/Crimson.Cli/Crimson.Cli.csproj --"
{{buildCommand}}
```

Project layout:

- `contracts/`: Crimson IDL contracts
- `cpp/generated/`: Crimson-generated C++ headers and sources
- `cpp/user/`: merge-protected user implementation stubs
- `app/`: consuming C++ entry point
- `.crimson/cmake/Crimson.Cpp.cmake`: tool-owned CMake integration helper
""";

    internal static string RenderCMakeLists(string projectName) => $$"""
cmake_minimum_required(VERSION 3.20)
project({{projectName}}App LANGUAGES CXX)

include("${CMAKE_CURRENT_SOURCE_DIR}/.crimson/cmake/Crimson.Cpp.cmake")

add_executable({{projectName}}App
    app/main.cpp
)

target_compile_features({{projectName}}App PRIVATE cxx_std_20)
crimson_configure_cpp_target({{projectName}}App)
""";

    internal const string StarterIdl = """
namespace SmartHome {
    /// Simple dimmable light.
    interface LightDevice {
        /// Display name shown to users.
        string display_name;

        /// Current brightness percentage.
        int32 brightness_percent = 35;
    }
}
""";

    internal const string DefaultMain = """
#include <iostream>

int main()
{
    std::cout << "Crimson C++ project ready." << std::endl;
    return 0;
}
""";

    internal const string StarterMain = """
#include <iostream>

#include "SmartHome/LightDevice.hpp"

int main()
{
    SmartHome::LightDevice light;
    light.SetDisplayName("Porch Light");
    light.SetBrightnessPercent(42);
    std::cout << light.GetDisplayName() << ": " << light.GetBrightnessPercent() << "%" << std::endl;
    return 0;
}
""";
}
