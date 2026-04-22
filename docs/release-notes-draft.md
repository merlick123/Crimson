# Crimson Release Notes Draft

This draft tracks the current release story for the repository and should stay aligned with the shipped CLI, built-in profiles, and example workflows.

## Highlights

- A neutral `.idl` language with ANTLR-based parsing, semantic validation, and JSON AST export.
- Built-in code generation for C#, C++, and Rust with staged, merge-aware materialization.
- Reusable host integrations for MSBuild, CMake, and Cargo consumers.
- Built-in init profiles for `csharp`, `cpp-cmake`, `cpp-cmake-gcc`, `cpp-cmake-cross`, `rust-cargo`, and `rust-cargo-no-std`.
- Project validation through `crimson validate`, plus improved CLI diagnostics and build workflow polish.
- SmartHomeDemo as the shared end-to-end example across the built-in frontends, including shared contracts, generated/user code separation, and a common runtime scenario.
- Generated C# customization hooks for value members.

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
- init profiles for `csharp`, `cpp-cmake`, `cpp-cmake-gcc`, `cpp-cmake-cross`, `rust-cargo`, and `rust-cargo-no-std`
- example-driven consumer workflows through `examples/SmartHomeDemo`

## Known Limits

- Merge resolution is still conservative and file-level.
- Interactive external merge tooling is not implemented yet.
- Built-in target and host coverage is still intentionally narrow to the currently supported C#/.NET, C++/CMake, and Rust/Cargo workflows.
- Higher-level generation planning such as flavors and deployment-driven selection is still future work.

## Suggested Install / Try

```bash
dotnet publish src/Crimson.Cli/Crimson.Cli.csproj -c Release -o .artifacts/crimson
export PATH="$PWD/.artifacts/crimson:$PATH"
crimson init-profiles
crimson validate examples/SmartHomeDemo/SmartHome.crimsonproj
dotnet run --project examples/SmartHomeDemo/app/SmartHomeDemo.App.csproj
```
