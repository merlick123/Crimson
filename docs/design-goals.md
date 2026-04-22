# Design Goals

Crimson is intended to grow from an initial IDL-to-C# vertical slice into a target-agnostic interface-definition and generation platform. This document captures the design goals that should guide that growth.

## Core Goals

1. Preserve a neutral semantic core.
2. Allow new output languages to be added without editing core orchestration logic.
3. Protect user-owned code during regeneration.
4. Keep the language model richer than any single initial target, without silently discarding semantics.
5. Favor explicit validation and diagnostics over implicit behavior.

## Architectural Boundaries

### Core owns

- grammar and parsing
- semantic model construction
- generic semantic validation
- project loading and source discovery
- target orchestration through neutral abstractions
- merge orchestration and generic merge infrastructure

### Targets own

- target-specific configuration parsing
- target-specific validation and capability checks
- emitted file layout
- code generation rules
- target integration artifacts such as build hooks or props/targets files

## Target Extensibility

Adding a new target should require:

- creating a new emitter implementation under its own folder
- registering that emitter with the workspace or composition root
- adding tests that prove the target works through the shared abstraction

Adding a new target should not require:

- changing the semantic model just to match one language's syntax
- adding hard-coded target branches in `CrimsonWorkspace`
- introducing target-specific concepts into generic validation

## Merge Model

Crimson uses staged generation plus merge application to avoid overwriting user intent.

Project-local state is split by concern:

- `.merge/previous`: prior staged baseline used for merge comparison
- `.merge/current`: newly generated staged output
- `.merge/backup`: backups made while applying merges
- `.crimson/`: non-merge tool assets only

The merge model should remain conservative. When in doubt, prefer a conflict over an unsafe automatic overwrite.

## Validation Philosophy

Validation should happen at the earliest layer that has enough information to make a correct decision.

- Generic semantic rules belong in core validation.
- Representability rules belong in the target.
- Unsupported target features should produce explicit diagnostics.

## Language Evolution

The IDL can evolve beyond the feature set of the first implemented target. That is acceptable as long as:

- the semantic model stays coherent
- unsupported target scenarios fail clearly
- tests cover the new behavior and its failure modes

## Maintainability Rules

- Keep abstractions small and named by responsibility.
- Avoid embedding file-layout assumptions into unrelated layers.
- Prefer additive extension points over switch statements that grow per target.
- Keep docs and tests updated when state layout or architectural boundaries change.
