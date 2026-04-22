namespace Crimson.Core.Projects;

public sealed class CSharpProjectInitProfile : IProjectInitProfile
{
    public string ProfileId => "csharp";

    public string DisplayName => "C# / .NET";

    public string Description => "C# output with generated/user source split and MSBuild integration.";

    public ProjectInitPlan CreatePlan(ProjectInitContext context)
    {
        var files = new List<ProjectInitFile>
        {
            new("README.md", RenderReadme(context.ProjectName)),
            new(Path.Combine("app", $"{context.ProjectName}.App.csproj"), RenderAppProject(context.ProjectName)),
            new(Path.Combine("app", "Program.cs"), context.Starter ? StarterProgram : DefaultProgram),
        };

        if (context.Starter)
        {
            files.Add(new ProjectInitFile(Path.Combine("contracts", "hello.idl"), StarterIdl));
        }

        return new ProjectInitPlan(
            [
                new ProjectInitGroup(
                    "csharp",
                    "csharp",
                    ["contracts/**/*.idl"],
                    Array.Empty<string>(),
                    "src",
                    new { },
                    new ProjectInitHost("dotnet-msbuild", new { projectDirectories = new[] { "app" } }))
            ],
            files);
    }

    private static string RenderReadme(string projectName) => $$"""
# {{projectName}}

This project uses Crimson with the `csharp` init profile.

Run it from this directory:

```bash
dotnet run --project app/{{projectName}}.App.csproj
```

The MSBuild integration runs `crimson build` automatically before compile.

Override the Crimson command if you are using a local repo build:

```bash
dotnet run --project app/{{projectName}}.App.csproj \
  -p:CrimsonCommand=dotnet \
  -p:CrimsonCommandArguments="run --project /path/to/src/Crimson.Cli/Crimson.Cli.csproj --"
```

Project layout:

- `contracts/`: Crimson IDL contracts
- `src/Generated/`: Crimson-generated C# output
- `src/User/`: merge-protected user implementation stubs
- `app/`: consuming .NET application
- `.crimson/msbuild/`: tool-owned MSBuild integration files
""";

    private static string RenderAppProject(string projectName) => $$"""
<Project Sdk="Microsoft.NET.Sdk">
  <Import Project="../.crimson/msbuild/Crimson.csharp.props" />

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <CrimsonProjectFile>../{{projectName}}.crimsonproj</CrimsonProjectFile>
  </PropertyGroup>

  <Import Project="../.crimson/msbuild/Crimson.csharp.targets" />
</Project>
""";

    private const string StarterIdl = """
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

    private const string DefaultProgram = """
Console.WriteLine("Crimson C# project ready.");
""";

    private const string StarterProgram = """
using SmartHome;

var light = new LightDevice
{
    DisplayName = "Porch Light",
    BrightnessPercent = 42,
};

Console.WriteLine($"{light.DisplayName}: {light.BrightnessPercent}%");
""";
}
