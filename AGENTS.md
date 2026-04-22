# AGENTS

This repository is designed to support both human contributors and coding agents. The goal is consistent changes with minimal architectural drift.

## Working Principles

- Keep the semantic core target- and host-agnostic. Parsing, semantic modeling, generic validation, merge infrastructure, and project loading should not depend on a specific output language or build tool. The default workspace may register built-in implementations, but orchestration should continue to flow through `ITargetEmitter`, `IHostIntegration`, and `IProjectInitProfile`.
- Put output-language behavior under that target's own folder. C# lives under `src/Crimson.Core/Generation/CSharp/`, C++ lives under `src/Crimson.Core/Generation/Cpp/`, and future targets should follow the same pattern.
- Keep parsing, semantic modeling, generic validation, merge infrastructure, and workspace orchestration in core. Push target-specific validation, emitted file layout, and code-shaping decisions into the emitter. Push build-system or toolchain integration files into host integrations. Keep project scaffolding and starter-file decisions in init profiles.
- Avoid silent semantic degradation. If the neutral language supports a construct that a target cannot represent, reject it in target validation rather than emitting a lossy approximation.
- Preserve user-owned files. Merge behavior is intentionally conservative; do not weaken that without tests covering conflict cases.

## Repository Conventions

- `grammar/Crimson.g4` defines the language grammar. If it changes, regenerate parser output in `src/Crimson.Core/Parsing/Generated/`.
- `src/Crimson.Core/Model/` holds the neutral semantic model.
- `src/Crimson.Core/Validation/` should remain target-independent unless validation is explicitly target capability checking behind an emitter interface.
- `src/Crimson.Core/Generation/` contains target abstractions and per-target emitters.
- `src/Crimson.Core/Host/` contains host/build integration abstractions and implementations for tool-owned project assets under `.crimson/`.
- `src/Crimson.Core/Projects/` contains project file loading, source discovery, and init-profile abstractions and implementations.
- `src/Crimson.Cli/` is the thin command-line entry point.
- `.merge/` under a project root is merge state only: `previous`, `current`, and `backup`.
- `.crimson/` under a project root is reserved for tool-owned assets such as host/build integration files and related tool state. Emitters should stage generated code through the merge pipeline rather than writing directly under `.crimson/`.
- Project initialization should flow through init profiles. New conceptual setups should be added as profiles rather than by hard-coding more `init` branches.

## Change Expectations

- Keep changes focused and reviewable.
- Add tests for semantic behavior changes, orchestration changes, and merge-state changes.
- If you add a new target, add at least:
  - emitter-level tests
  - workspace orchestration coverage proving the target works without core special-casing
  - an end-to-end scenario if the target has project integration behavior
- If you add a new host integration or init profile, add workspace-level coverage proving it composes through the shared abstractions, plus an end-to-end scenario if it writes or consumes project assets.
- When agent-made changes leave the repository at a stable point, the agent should recommend a git commit instead of leaving the checkpoint implicit.
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
