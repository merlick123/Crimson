namespace Crimson.Core.Generation.CSharp;

public static class CSharpBuildIntegration
{
    public static void Write(string projectDirectory, CSharpTargetOptions options)
    {
        var msbuildRoot = Path.Combine(projectDirectory, ".crimson", "msbuild");
        Directory.CreateDirectory(msbuildRoot);
        File.WriteAllText(Path.Combine(msbuildRoot, "Crimson.CSharp.props"), RenderPropsFile(options));
        File.WriteAllText(Path.Combine(msbuildRoot, "Crimson.CSharp.targets"), TargetsFile);
    }

    private static string RenderPropsFile(CSharpTargetOptions options)
    {
        var outputRoot = EscapeXml(Crimson.Core.Utility.PathHelpers.NormalizeRelativePath(options.OutputRoot));
        return $$"""
<Project>
  <PropertyGroup>
    <CrimsonCommand Condition="'$(CrimsonCommand)' == ''">crimson</CrimsonCommand>
    <CrimsonCommandArguments Condition="'$(CrimsonCommandArguments)' == ''"></CrimsonCommandArguments>
    <CrimsonSourceRoot Condition="'$(CrimsonSourceRoot)' == ''">$(MSBuildThisFileDirectory)../../{{outputRoot}}</CrimsonSourceRoot>
    <NoWarn>$(NoWarn);CS2002</NoWarn>
  </PropertyGroup>
</Project>
""";
    }

    private const string TargetsFile = """
<Project>
  <Target Name="RunCrimsonBuild"
          BeforeTargets="CoreCompile"
          Condition="'$(CrimsonProjectFile)' != ''">
    <Exec Command="&quot;$(CrimsonCommand)&quot; $(CrimsonCommandArguments) build &quot;$(CrimsonProjectFile)&quot;"
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

    private static string EscapeXml(string value) =>
        value.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);
}
