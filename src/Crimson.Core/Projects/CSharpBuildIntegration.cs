namespace Crimson.Core.Projects;

public static class CSharpBuildIntegration
{
    public static void Write(string projectDirectory)
    {
        var msbuildRoot = Path.Combine(projectDirectory, ".crimson", "msbuild");
        Directory.CreateDirectory(msbuildRoot);
        File.WriteAllText(Path.Combine(msbuildRoot, "Crimson.CSharp.props"), PropsFile);
        File.WriteAllText(Path.Combine(msbuildRoot, "Crimson.CSharp.targets"), TargetsFile);
    }

    private const string PropsFile = """
<Project>
  <PropertyGroup>
    <CrimsonCommand Condition="'$(CrimsonCommand)' == ''">crimson</CrimsonCommand>
    <CrimsonSourceRoot Condition="'$(CrimsonSourceRoot)' == ''">../src</CrimsonSourceRoot>
    <NoWarn>$(NoWarn);CS2002</NoWarn>
  </PropertyGroup>
</Project>
""";

    private const string TargetsFile = """
<Project>
  <Target Name="RunCrimsonBuild"
          BeforeTargets="CoreCompile"
          Condition="'$(CrimsonProjectFile)' != ''">
    <Exec Command="&quot;$(CrimsonCommand)&quot; build &quot;$(CrimsonProjectFile)&quot;"
          WorkingDirectory="$(MSBuildProjectDirectory)" />
  </Target>

  <Target Name="IncludeCrimsonGeneratedSources"
          BeforeTargets="CoreCompile"
          DependsOnTargets="RunCrimsonBuild"
          Condition="'$(CrimsonProjectFile)' != '' and '$(CrimsonSourcesIncluded)' != 'true'">
    <ItemGroup>
      <Compile Remove="$(CrimsonSourceRoot)/Generated/**/*.cs" />
      <Compile Remove="$(CrimsonSourceRoot)/User/**/*.cs" />
      <Compile Include="$(CrimsonSourceRoot)/Generated/**/*.cs" Link="Generated/%(RecursiveDir)%(Filename)%(Extension)" />
      <Compile Include="$(CrimsonSourceRoot)/User/**/*.cs" Link="User/%(RecursiveDir)%(Filename)%(Extension)" />
    </ItemGroup>
    <PropertyGroup>
      <CrimsonSourcesIncluded>true</CrimsonSourcesIncluded>
    </PropertyGroup>
  </Target>
</Project>
""";
}
