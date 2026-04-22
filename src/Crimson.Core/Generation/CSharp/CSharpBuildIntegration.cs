namespace Crimson.Core.Generation.CSharp;

public static class CSharpBuildIntegration
{
    public static void Write(string projectDirectory, string groupName, CSharpTargetOptions options)
    {
        var msbuildRoot = Path.Combine(projectDirectory, ".crimson", "msbuild");
        var filePrefix = SanitizeGroupName(groupName);
        Directory.CreateDirectory(msbuildRoot);
        File.WriteAllText(Path.Combine(msbuildRoot, $"Crimson.{filePrefix}.props"), RenderPropsFile(groupName, options));
        File.WriteAllText(Path.Combine(msbuildRoot, $"Crimson.{filePrefix}.targets"), RenderTargetsFile(groupName));
    }

    private static string RenderPropsFile(string groupName, CSharpTargetOptions options)
    {
        var identifier = SanitizeGroupName(groupName);
        var outputRoot = EscapeXml(Crimson.Core.Utility.PathHelpers.NormalizeRelativePath(options.OutputRoot));
        return $$"""
<Project>
  <PropertyGroup>
    <CrimsonCommand Condition="'$(CrimsonCommand)' == ''">crimson</CrimsonCommand>
    <CrimsonCommandArguments Condition="'$(CrimsonCommandArguments)' == ''"></CrimsonCommandArguments>
    <Crimson{{identifier}}SourceRoot Condition="'$(Crimson{{identifier}}SourceRoot)' == ''">$(MSBuildThisFileDirectory)../../{{outputRoot}}</Crimson{{identifier}}SourceRoot>
    <NoWarn>$(NoWarn);CS2002</NoWarn>
  </PropertyGroup>
</Project>
""";
    }

    private static string RenderTargetsFile(string groupName)
    {
        var identifier = SanitizeGroupName(groupName);
        return $$"""
<Project>
  <Target Name="RunCrimsonBuild_{{identifier}}"
          BeforeTargets="CoreCompile"
          Condition="'$(CrimsonProjectFile)' != ''">
    <Exec Command="&quot;$(CrimsonCommand)&quot; $(CrimsonCommandArguments) build &quot;$(CrimsonProjectFile)&quot;"
          WorkingDirectory="$(MSBuildProjectDirectory)" />
  </Target>

  <Target Name="IncludeCrimsonGeneratedSources_{{identifier}}"
          BeforeTargets="CoreCompile"
          DependsOnTargets="RunCrimsonBuild_{{identifier}}"
          Condition="'$(CrimsonProjectFile)' != '' and '$(Crimson{{identifier}}SourcesIncluded)' != 'true'">
    <ItemGroup>
      <Compile Remove="$(Crimson{{identifier}}SourceRoot)/Generated/**/*.cs" />
      <Compile Remove="$(Crimson{{identifier}}SourceRoot)/User/**/*.cs" />
      <Compile Include="$(Crimson{{identifier}}SourceRoot)/Generated/**/*.cs" Link="Generated/%(RecursiveDir)%(Filename)%(Extension)" />
      <Compile Include="$(Crimson{{identifier}}SourceRoot)/User/**/*.cs" Link="User/%(RecursiveDir)%(Filename)%(Extension)" />
    </ItemGroup>
    <PropertyGroup>
      <Crimson{{identifier}}SourcesIncluded>true</Crimson{{identifier}}SourcesIncluded>
    </PropertyGroup>
  </Target>
</Project>
""";
    }

    private static string EscapeXml(string value) =>
        value.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);

    private static string SanitizeGroupName(string groupName)
    {
        var builder = new System.Text.StringBuilder(groupName.Length);
        foreach (var character in groupName)
        {
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');
        }

        return builder.Length == 0 ? "Group" : builder.ToString();
    }
}
