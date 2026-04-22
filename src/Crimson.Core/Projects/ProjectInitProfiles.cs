namespace Crimson.Core.Projects;

public sealed record ProjectInitContext(
    string ProjectFilePath,
    string ProjectDirectory,
    string ProjectName,
    bool Starter);

public sealed record ProjectInitTarget(string TargetName, object Configuration);

public sealed record ProjectInitHost(string HostName, object Configuration);

public sealed record ProjectInitFile(string RelativePath, string Content);

public sealed record ProjectInitPlan(
    IReadOnlyList<string> Sources,
    IReadOnlyList<string> Excludes,
    IReadOnlyList<ProjectInitTarget> Targets,
    ProjectInitHost? Host,
    IReadOnlyList<ProjectInitFile> Files);

public sealed record ProjectInitProfileInfo(
    string ProfileId,
    string DisplayName,
    string Description);

public interface IProjectInitProfile
{
    string ProfileId { get; }

    string DisplayName { get; }

    string Description { get; }

    ProjectInitPlan CreatePlan(ProjectInitContext context);
}
