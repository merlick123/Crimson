# Release Checklist

Use this checklist before cutting a tagged Crimson release.

## Product

- The README matches the current CLI and example workflow.
- `examples/SmartHomeDemo` still demonstrates the intended main C# example workflow:
  - generated/user code split
  - automatic C# build integration
  - capability-based swappability through `IDevice` and `IDemoHomeRuntime`
  - feature discovery and device-chain tracing
- The documented product scope is consistent across `README.md`, `docs/release-notes-draft.md`, and CLI help:
  - built-in targets are C# and C++
  - built-in host integrations are MSBuild and CMake
  - built-in init profiles are `csharp`, `cpp-cmake`, and `cpp-cmake-gcc`

## Verification

- `dotnet build src/Crimson.Core/Crimson.Core.csproj -c Release`
- `dotnet build src/Crimson.Cli/Crimson.Cli.csproj -c Release`
- `dotnet run --project tests/Crimson.Tests/Crimson.Tests.csproj`
- `dotnet run --project tests/Crimson.SystemTests/Crimson.SystemTests.csproj`
- `dotnet publish src/Crimson.Cli/Crimson.Cli.csproj -c Release -o .artifacts/crimson`
- `PATH="$PWD/.artifacts/crimson:$PATH" crimson init-profiles`
- `PATH="$PWD/.artifacts/crimson:$PATH" crimson validate examples/SmartHomeDemo/SmartHome.crimsonproj`
- `PATH="$PWD/.artifacts/crimson:$PATH" dotnet run --project examples/SmartHomeDemo/app/SmartHomeDemo.App.csproj`

## Repository

- The working tree is clean.
- CI passes on `main`.
- The license, notice, contributing guide, and code of conduct are present.
- No local-only scratch files or generated noise are staged.

## Release Assets

- Release binaries are being produced successfully by `release-artifacts`.
- The GitHub release description has been reviewed.
- The version/tag name has been chosen intentionally.

## Decision

- Review is complete.
- The release is approved for tagging.
