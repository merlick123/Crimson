# AGENTS

This repository is designed to support both human contributors and coding agents. The goal is consistent changes with minimal architectural drift.

## Working Principles

- Keep the core target-agnostic. `Crimson.Core` should not take a compile-time dependency on any specific output language concept beyond the neutral target-emitter abstraction.
- Put output-language behavior under that target's own folder. C# lives under `src/Crimson.Core/Generation/CSharp/`; future targets should follow the same pattern.
- Keep parsing, semantic modeling, generic validation, and workspace orchestration in core. Push target-specific validation, file layout, integration files, and code-shaping decisions into the emitter.
- Avoid silent semantic degradation. If the neutral language supports a construct that a target cannot represent, reject it in target validation rather than emitting a lossy approximation.
- Preserve user-owned files. Merge behavior is intentionally conservative; do not weaken that without tests covering conflict cases.

## Repository Conventions

- `grammar/Crimson.g4` defines the language grammar. If it changes, regenerate parser output in `src/Crimson.Core/Parsing/Generated/`.
- `src/Crimson.Core/Model/` holds the neutral semantic model.
- `src/Crimson.Core/Validation/` should remain target-independent unless validation is explicitly target capability checking behind an emitter interface.
- `src/Crimson.Core/Generation/` contains target abstractions and per-target emitters.
- `src/Crimson.Cli/` is the thin command-line entry point.
- `.merge/` under a project root is merge state only: `previous`, `current`, and `backup`.
- `.crimson/` under a project root is reserved for non-merge tool-owned assets such as build integration files.

## Change Expectations

- Keep changes focused and reviewable.
- Add tests for semantic behavior changes, orchestration changes, and merge-state changes.
- If you add a new target, add at least:
  - emitter-level tests
  - workspace orchestration coverage proving the target works without core special-casing
  - an end-to-end scenario if the target has project integration behavior
- Do not mix unrelated generated churn with logic changes.

## Verification

Before considering a change complete, run:

```bash
dotnet run --project tests/Crimson.Tests/Crimson.Tests.csproj
dotnet run --project tests/Crimson.SystemTests/Crimson.SystemTests.csproj
```

If the change affects the example app or project integration, also run:

```bash
dotnet run --project examples/SmartHomeDemo/app/SmartHomeDemo.App.csproj
```
