namespace Crimson.Core.Projects;

public sealed class CppCMakeGccProjectInitProfile : IProjectInitProfile
{
    public string ProfileId => "cpp-cmake-gcc";

    public string DisplayName => "C++ / CMake / GCC";

    public string Description => "C++ output with CMake integration and GCC-oriented presets.";

    public ProjectInitPlan CreatePlan(ProjectInitContext context)
    {
        var files = new List<ProjectInitFile>
        {
            new("README.md", CppCMakeProjectInitProfile.RenderReadme(context.ProjectName, "cpp-cmake-gcc", "cmake --preset gcc-debug", "cmake --build --preset gcc-debug", "The preset is wired for GCC and the generated CMake module runs `crimson build` automatically.")),
            new("CMakeLists.txt", CppCMakeProjectInitProfile.RenderCMakeLists(context.ProjectName)),
            new("CMakePresets.json", CMakePresets),
            new(Path.Combine("app", "main.cpp"), context.Starter ? CppCMakeProjectInitProfile.StarterMain : CppCMakeProjectInitProfile.DefaultMain),
        };

        if (context.Starter)
        {
            files.Add(new ProjectInitFile(Path.Combine("contracts", "hello.idl"), CppCMakeProjectInitProfile.StarterIdl));
        }

        return new ProjectInitPlan(
            ["contracts/**/*.idl"],
            Array.Empty<string>(),
            [new ProjectInitTarget("cpp", new { output = "cpp" })],
            new ProjectInitHost("cmake", new { buildDirectory = "build" }),
            files);
    }

    private const string CMakePresets = """
{
  "version": 3,
  "configurePresets": [
    {
      "name": "gcc-debug",
      "generator": "Unix Makefiles",
      "binaryDir": "${sourceDir}/build/gcc-debug",
      "cacheVariables": {
        "CMAKE_BUILD_TYPE": "Debug",
        "CMAKE_C_COMPILER": "gcc",
        "CMAKE_CXX_COMPILER": "g++"
      }
    }
  ],
  "buildPresets": [
    {
      "name": "gcc-debug",
      "configurePreset": "gcc-debug"
    }
  ]
}
""";
}
