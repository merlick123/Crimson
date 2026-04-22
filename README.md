# Crimson

Crimson is an interface-definition and code-generation framework.

Current scope in this repository:

- a neutral `.idl` language with familiar declaration syntax
- an ANTLR-based parser that lowers `.idl` into a typed semantic model
- semantic validation and diagnostics
- JSON AST export
- built-in target-language generators
- staged generation and conservative 3-way merge scaffolding
- reusable MSBuild, CMake, and Cargo host integrations
- CLI commands for `init`, `parse`, `validate`, `generate`, `merge`, and `build`

For the IDL syntax and semantics, see `docs/idl-reference.md`.

## Status

This is an early implementation.

The current end-to-end workflows are working, but the full design space discussed for Crimson is not complete yet. In particular:

- merge resolution is conservative and file-level
- interactive external merge-tool support is not implemented yet
- built-in target coverage is still intentionally narrow
- higher-level generation planning such as flavors and deployment-driven output selection is not implemented yet

## Repository Layout

```txt
grammar/                 ANTLR grammar for the IDL
src/Crimson.Core/        parser, semantic model, generation, merge logic
src/Crimson.Cli/         command-line entry point
tests/Crimson.Tests/     focused parser/generator checks
tests/Crimson.SystemTests/ end-to-end project/build checks
examples/                sample Crimson projects
tools/                   local tooling artifacts needed by the build
```

## Requirements

- .NET SDK 10.0 or later
- Java 21 or later

## Quick Start

After building or installing the `crimson` CLI, initialize a new project:

```bash
crimson init Demo --profile csharp --starter
```

List available init profiles:

```bash
crimson init-profiles
```

The built-in profiles currently include `csharp`, `cpp-cmake`, `cpp-cmake-gcc`, `cpp-cmake-cross`, `rust-cargo`, and `rust-cargo-no-std`.

Build the included example projects:

```bash
crimson build examples/SmartHomeDemo/SmartHome.crimsonproj
crimson build examples/SmartHomeDemo/SmartHome.Cpp.crimsonproj
crimson build examples/SmartHomeDemo/SmartHome.Rust.crimsonproj
```

Run the shared SmartHome frontends:

```bash
dotnet run --project examples/SmartHomeDemo/app/SmartHomeDemo.App.csproj
cmake --preset gcc-debug -S examples/SmartHomeDemo -B examples/SmartHomeDemo/build/gcc-debug \
  -DCrimsonCommand=dotnet \
  -DCrimsonCommandArguments="run --project $PWD/src/Crimson.Cli/Crimson.Cli.csproj --"
cmake --build examples/SmartHomeDemo/build/gcc-debug
./examples/SmartHomeDemo/build/gcc-debug/SmartHomeCppApp
cargo run --manifest-path examples/SmartHomeDemo/Cargo.toml
```

Validate a project:

```bash
crimson validate examples/SmartHomeDemo/SmartHome.crimsonproj
```

Parse an IDL file to JSON:

```bash
crimson parse examples/SmartHomeDemo/contracts/core/smart_home.idl
```

Value-only types should be declared as `struct`:

```idl
namespace SmartHome {
    enum SceneMode {
        Home,
        Away,
    }

    struct ClimateSnapshot {
        float64 indoor_temperature_c;
        float64 target_temperature_c;
    }

    interface DemoHomeRuntime {
        SceneMode active_mode = Home;
        ClimateSnapshot latest_climate;
    }
}
```

## Build From Source

To build a runnable Crimson CLI from source for your own machine:

```bash
dotnet publish src/Crimson.Cli/Crimson.Cli.csproj -c Release -o .artifacts/crimson
```

That produces a local build of the `crimson` CLI under `.artifacts/crimson/`.

On Unix-like systems, the published executable is `crimson`. On Windows, it is `crimson.exe`.

If you want a single self-contained executable for a specific platform:

```bash
dotnet publish src/Crimson.Cli/Crimson.Cli.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -o .artifacts/crimson-linux-x64
```

You can then add the published directory to your `PATH` or symlink the resulting executable.

Helper scripts are included:

```bash
./scripts/publish.sh
./scripts/publish-self-contained.sh linux-x64
```

For fast local iteration, `./scripts/publish.sh` also refreshes a stable dev link at `.artifacts/bin/crimson`.
To use that build in the current shell without changing your login profile:

```bash
export PATH="$PWD/.artifacts/bin:$PATH"
```

Or print the export command directly:

```bash
eval "$(./scripts/use-local-build.sh)"
```

Common runtime identifiers:

- `linux-x64`
- `win-x64`
- `osx-arm64`
- `osx-x64`

For generic cross-compiled C++ projects, the `cpp-cmake-cross` init profile writes a reusable `cmake/toolchains/generic.cmake` scaffold and a matching `cross-debug` CMake preset.

## Releases

The intended distribution model is:

- developers can build Crimson from source with `dotnet publish`
- release binaries can be published as GitHub release artifacts for supported platforms

## Development

Crimson contributors should use the workflow documented in `CONTRIBUTING.md`.

Build the main projects:

```bash
dotnet build src/Crimson.Core/Crimson.Core.csproj
dotnet build src/Crimson.Cli/Crimson.Cli.csproj
```

Run verification:

```bash
dotnet run --project tests/Crimson.Tests/Crimson.Tests.csproj
dotnet run --project tests/Crimson.SystemTests/Crimson.SystemTests.csproj
```

Publish a local release-style build:

```bash
./scripts/publish.sh
```

Regenerate the parser after changing the grammar:

```bash
mkdir -p src/Crimson.Core/Parsing/Generated
java -jar tools/antlr-4.13.1-complete.jar \
  -Dlanguage=CSharp \
  -visitor \
  -no-listener \
  -package Crimson.Core.Parsing.Generated \
  -o src/Crimson.Core/Parsing/Generated \
  grammar/Crimson.g4
```

If `tools/antlr-4.13.1-complete.jar` is missing, download it from the official ANTLR site:

```bash
mkdir -p tools
curl -L -o tools/antlr-4.13.1-complete.jar https://www.antlr.org/download/antlr-4.13.1-complete.jar
```

## Example

The main example project is `examples/SmartHomeDemo`.

`examples/SmartHomeDemo` demonstrates:

- one shared contract tree lowered into multiple target-language frontends
- generated and user-owned code under `src/`, `cpp/`, and `rust/src/`
- automatic Crimson-triggered regeneration from consuming .NET, CMake, and Cargo frontends
- capability-based swappability across vendor devices through `IDevice` and related interfaces
- querying device features and tracing automation chains across the home

## Contributing

See `CONTRIBUTING.md`.

## Design Notes

Project design goals are documented in `docs/design-goals.md`.

## Security

See `SECURITY.md`.

## License

Crimson is licensed under the Apache License 2.0. See `LICENSE` and `NOTICE`.
