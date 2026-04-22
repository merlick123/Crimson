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
            new(Path.Combine("app", $"{context.ProjectName}.App.csproj"), RenderAppProject(context.ProjectName)),
            new(Path.Combine("app", "Program.cs"), context.Starter ? StarterProgram : DefaultProgram),
        };

        if (context.Starter)
        {
            files.Add(new ProjectInitFile(Path.Combine("contracts", "hello.idl"), StarterIdl));
        }

        return new ProjectInitPlan(
            ["contracts/**/*.idl"],
            Array.Empty<string>(),
            [new ProjectInitTarget("csharp", new { output = "src" })],
            new ProjectInitHost("dotnet-msbuild", new { projectDirectories = new[] { "app" } }),
            files);
    }

    private static string RenderAppProject(string projectName) => $$"""
<Project Sdk="Microsoft.NET.Sdk">
  <Import Project="../.crimson/msbuild/Crimson.CSharp.props" />

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <CrimsonProjectFile>../{{projectName}}.crimsonproj</CrimsonProjectFile>
  </PropertyGroup>

  <Import Project="../.crimson/msbuild/Crimson.CSharp.targets" />
</Project>
""";

    private const string StarterIdl = """
namespace SmartHome {
    /// Example smart-home device.
    interface LightDevice {
        /// Human-friendly device name.
        string display_name;

        /// Current brightness level.
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
