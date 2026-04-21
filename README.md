# Crimson

Crimson is an interface-definition and code-generation framework.

Current scope in this repository:

- a neutral `.idl` language with familiar declaration syntax
- an ANTLR-based parser that lowers `.idl` into a typed semantic model
- semantic validation and diagnostics
- JSON AST export
- an initial C# generator
- staged generation and conservative 3-way merge scaffolding
- reusable MSBuild integration for C# consumers
- CLI commands for `init`, `parse`, `validate`, `generate`, `merge`, and `build`

## Status

This is an early implementation.

The current vertical slice is working, but the full design space discussed for Crimson is not complete yet. In particular:

- merge resolution is conservative and file-level
- interactive external merge-tool support is not implemented yet
- only the C# target is currently implemented
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
crimson init Demo --starter
```

Build the included example project:

```bash
crimson build examples/BillingDemo/Billing.crimsonproj
```

Validate a project:

```bash
crimson validate examples/BillingDemo/Billing.crimsonproj
```

Parse an IDL file to JSON:

```bash
crimson parse examples/BillingDemo/contracts/customer.idl
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

Common runtime identifiers:

- `linux-x64`
- `win-x64`
- `osx-arm64`
- `osx-x64`

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

An example project is included in `examples/BillingDemo`.

It demonstrates:

- generated C# interfaces and class plumbing
- user-owned code under `src/User`
- automatic Crimson-triggered regeneration from a consuming C# project
- swapping two implementations behind `ICustomerService`

## Contributing

See `CONTRIBUTING.md`.

## Security

See `SECURITY.md`.

## License

Crimson is licensed under the Apache License 2.0. See `LICENSE` and `NOTICE`.
