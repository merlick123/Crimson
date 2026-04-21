# Crimson

Crimson is an interface-definition and code-generation framework.

Current scope in this repository:

- a neutral `.idl` language with familiar declaration syntax
- an ANTLR-based parser that lowers `.idl` into a typed semantic model
- JSON AST export
- an initial C# generator
- staged generation and conservative 3-way merge scaffolding
- CLI commands for `init`, `parse`, `generate`, `merge`, and `build`

## Status

This is an early implementation.

The current vertical slice is working, but the full design space discussed for Crimson is not complete yet. In particular:

- validation is still shallow
- merge resolution is conservative and file-level
- interactive external merge-tool support is not implemented yet
- only the C# target is currently implemented

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

Initialize a new project:

```bash
dotnet run --project src/Crimson.Cli/Crimson.Cli.csproj -- init Demo.crimsonproj --starter
```

Parse an IDL file to JSON:

```bash
dotnet run --project src/Crimson.Cli/Crimson.Cli.csproj -- parse examples/BillingDemo/contracts/customer.idl
```

Build the included example project:

```bash
dotnet run --project src/Crimson.Cli/Crimson.Cli.csproj -- build examples/BillingDemo/Billing.crimsonproj
```

## Development

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

## Contributing

See `CONTRIBUTING.md`.

## Security

See `SECURITY.md`.

## License

Crimson is licensed under the Apache License 2.0. See `LICENSE` and `NOTICE`.
