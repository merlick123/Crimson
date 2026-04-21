# Crimson Release Notes Draft

This draft is intended for the first reviewed public release after the current first-release hardening work.

## Highlights

- Added semantic project validation with `crimson validate`.
- Improved CLI diagnostics and build workflow polish.
- Extracted reusable C# MSBuild integration for Crimson-generated projects.
- Replaced the old sample with SmartHomeDemo, a stronger end-to-end example with:
  - automatic Crimson-triggered regeneration
  - generated/user-owned C# split
  - swappable implementations through `IHomeController`
  - feature queries and automation-chain tracing
- Tightened generated C# customization hooks for value members.

## Current Scope

Crimson currently focuses on a coherent C# vertical slice:

- `.idl` parsing
- semantic model + JSON AST export
- validation
- C# generation
- staged merge-aware materialization
- example-driven C# consumer workflow

## Known Limits

- Only the C# target is implemented.
- Merge resolution is still conservative and file-level.
- Interactive external merge tooling is not implemented yet.
- Higher-level generation planning such as flavors and deployment-driven selection is still future work.

## Suggested Install / Try

```bash
dotnet publish src/Crimson.Cli/Crimson.Cli.csproj -c Release -o .artifacts/crimson
export PATH="$PWD/.artifacts/crimson:$PATH"
crimson validate examples/SmartHomeDemo/SmartHome.crimsonproj
dotnet run --project examples/SmartHomeDemo/app/SmartHomeDemo.App.csproj
```
