# Crimson Release Notes Draft

This draft tracks the current release story for the repository and should stay aligned with the shipped CLI, built-in profiles, and example workflows.

## Highlights

- Added a built-in C++ target alongside the existing C# workflow.
- Added a built-in Rust target with Cargo integration and Rust init profiles.
- Added reusable CMake integration for C++ consumers.
- Added built-in init profiles for `csharp`, `cpp-cmake`, `cpp-cmake-gcc`, `rust-cargo`, and `rust-cargo-no-std`.
- Added semantic project validation with `crimson validate`.
- Improved CLI diagnostics and build workflow polish.
- Extracted reusable C# MSBuild integration for Crimson-generated projects.
- Kept SmartHomeDemo as the main end-to-end C# example with:
  - automatic Crimson-triggered regeneration
  - generated/user-owned C# split
  - capability-based swappability through `IDevice` and `IDemoHomeRuntime`
  - feature queries and automation-chain tracing
- Tightened generated C# customization hooks for value members.

## Current Scope

Crimson currently includes these built-in workflows:

- `.idl` parsing
- semantic model + JSON AST export
- validation
- C# generation
- C++ generation
- Rust generation
- staged merge-aware materialization
- MSBuild integration for C# consumers
- CMake integration for C++ consumers
- Cargo integration for Rust consumers
- init profiles for `csharp`, `cpp-cmake`, `cpp-cmake-gcc`, `rust-cargo`, and `rust-cargo-no-std`
- example-driven C# consumer workflow through `examples/SmartHomeDemo`
- example-driven Rust consumer workflow through `examples/RustDeviceDemo`

## Known Limits

- Merge resolution is still conservative and file-level.
- Interactive external merge tooling is not implemented yet.
- Built-in target and host coverage is still intentionally narrow to the currently supported C#/.NET and C++/CMake workflows.
- Higher-level generation planning such as flavors and deployment-driven selection is still future work.

## Suggested Install / Try

```bash
dotnet publish src/Crimson.Cli/Crimson.Cli.csproj -c Release -o .artifacts/crimson
export PATH="$PWD/.artifacts/crimson:$PATH"
crimson init-profiles
crimson validate examples/SmartHomeDemo/SmartHome.crimsonproj
dotnet run --project examples/SmartHomeDemo/app/SmartHomeDemo.App.csproj
```
