# Contributing

## Ground Rules

- Keep changes focused.
- Prefer small, reviewable pull requests.
- Do not mix generated output churn with unrelated logic changes.
- If you change `grammar/Crimson.g4`, regenerate the parser output in `src/Crimson.Core/Parsing/Generated/`.

## Development Setup

Required tools:

- .NET SDK 10.0 or later
- Java 21 or later

## Running From Source

For source-level iteration during development:

```bash
dotnet run --project src/Crimson.Cli/Crimson.Cli.csproj -- parse examples/SmartHomeDemo/contracts/core/smart_home.idl
dotnet run --project src/Crimson.Cli/Crimson.Cli.csproj -- validate examples/SmartHomeDemo/SmartHome.crimsonproj
dotnet run --project src/Crimson.Cli/Crimson.Cli.csproj -- build examples/SmartHomeDemo/SmartHome.crimsonproj
```

Build:

```bash
dotnet build src/Crimson.Core/Crimson.Core.csproj
dotnet build src/Crimson.Cli/Crimson.Cli.csproj
```

Publish a runnable local CLI build:

```bash
./scripts/publish.sh
```

For quick rebuild-and-run cycles, add the stable dev link directory to `PATH` for the current shell:

```bash
eval "$(./scripts/use-local-build.sh)"
```

Or publish a self-contained single-file build for a target runtime:

```bash
./scripts/publish-self-contained.sh linux-x64
```

Run tests:

```bash
dotnet run --project tests/Crimson.Tests/Crimson.Tests.csproj
dotnet run --project tests/Crimson.SystemTests/Crimson.SystemTests.csproj
```

If the change affects the example app or project integration, also run:

```bash
dotnet run --project examples/SmartHomeDemo/app/SmartHomeDemo.App.csproj
```

## Parser Regeneration

After editing the grammar:

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

If the jar is not already present, fetch the exact version from the official ANTLR download site:

```bash
mkdir -p tools
curl -L -o tools/antlr-4.13.1-complete.jar https://www.antlr.org/download/antlr-4.13.1-complete.jar
```

## Pull Requests

Please include:

- the problem being solved
- the approach taken
- any follow-up work still missing
- verification steps you ran

If the change affects the language, parser, generator, or merge semantics, add or update both targeted tests and an end-to-end scenario where appropriate.
