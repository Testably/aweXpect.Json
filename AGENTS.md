# aweXpect.Json Agent Guide

aweXpect.Json is an extension package for [aweXpect](https://github.com/Testably/aweXpect) with expectations for JSON strings, `JsonElement`, `JsonNode` and the JSON serialization of objects with `System.Text.Json`.

The SDK is pinned in `global.json` (10.0.300, `latestPatch`). Package versions are managed centrally in `Directory.Packages.props`. Warnings are errors in `Source/`; a missing XML doc (CS1591) only warns, but every public member gets one.

## Design Goals

Correctness is a must. The other goals are strong defaults and win in this order when they conflict: stable and compatible, AOT and trimming friendly, a consistent and pleasant API, a fast happy path, extensible, errors at compile time, documented. [Docs/agents/design-goals.md](Docs/agents/design-goals.md) explains each goal, the code rules, the checks before a commit and the goals of this repository.

## Project Constraints

- The library targets `netstandard2.0;net8.0;net10.0` (`Source/Directory.Build.props`). Code that needs an API missing on `netstandard2.0` uses `#if` or a polyfill in `Source/aweXpect.Json/Helpers/Polyfills/`. Nullable is only `annotations` on `netstandard2.0`.
- The library depends on `aweXpect.Core` only, plus `System.Text.Json` on `netstandard2.0`. The `aweXpect.Core` version in `Directory.Packages.props` is the minimum consumers get, so raise it only when a Core feature is needed. `aweXpect` itself is a test dependency. Do not add new runtime dependencies.
- Preserve trimming and Native AOT compatibility (`IsTrimmable`/`IsAotCompatible` on `net8.0+`; only the `net10.0` build runs the analyzers). Reflection-based serialization of caller values must stay behind `ReflectionFallback.IsSupported` and throw `ReflectionFallbackHelpers.SerializationNotSupported(...)` otherwise. A new reflective path needs a check in `Tests/Aot/aweXpect.Json.Aot/Checks.cs`.
- The public API is fixed by snapshots per target framework. Breaking changes use `feat!:` and are deliberate.
- New expectations follow the existing shape: one partial file per expectation (`ThatJsonElement.IsObject.cs`), the same for each subject type that supports it (`JsonElement`, `JsonElement?`, `JsonNode`, `string`), with failure messages consistent with aweXpect.
- Assemblies are strong-named with `strongname.snk`.

## Validation

- Run the relevant tests before pushing. Tests run on `net8.0`, `net10.0` and `net48`; check `net48` when using newer language features (records work through the `IsExternalInit` polyfill).
- All tests run on TUnit and the Microsoft.Testing.Platform, which `global.json` selects for `dotnet test`: pass the project with `--project` and filter with `--treenode-filter`.
- Public API changes: run `Tests/aweXpect.Json.Api.Tests`, accept intentional changes with the explicit `AcceptApiChanges` test or `AWEXPECT_ACCEPT_SNAPSHOTS=all`, and commit the updated `*.awexpect-snapshot.txt` files. Never commit pending snapshots.
- Every failure test pins the full message with `.WithMessage(...)`, never a bare `Throws<T>()`.
- Behaviour changes in AOT-relevant code: run the AOT smoke tests (`./build.ps1 AotSmokeTests`).
- Never run `build.ps1`/`build.sh` without a target: the defaults are `ApiChecks`, `Benchmarks` and `CodeAnalysis`.
- New or changed expectations update `README.md` (overview table) and the page in `Docs/pages/`.

## Conventions

- Files use tabs and CRLF in the working tree (`.editorconfig`); git stores LF (`* text=auto`). Raw string literals in tests take the file's line endings, so edit with tools that keep them (prefer the Edit tool over `sed -i`).
- Most `.cs` files start with a UTF-8 BOM; keep a file's BOM as it is. PowerShell rewrites strip it, so check `git diff` for a lost or added U+FEFF.
- No em-dashes in `Docs/`, `README.md` or XML docs.

## Git Workflow

- Work on a branch and open a PR against `main`. PRs are squash-merged, so the commit subject is the PR title and follows [conventional commits](https://www.conventionalcommits.org/) (`feat:`, `fix:`, `docs:`, `test:`, `ci:`, `chore:`, `refactor:`, `feat!:` for breaking changes).
- The body explains why, not what; each paragraph on a single line. No co-author or attribution trailers.
- Releases are tags `vX.Y.Z` on `main`; do not create them.

## References

- [Docs/agents/design-goals.md](Docs/agents/design-goals.md): the goals and code rules for every change, the checks before a commit and the goals of this repository.
- [Docs/agents/workflows.md](Docs/agents/workflows.md): build, test, benchmark, mutation and documentation commands, pipeline targets and CI workflows.
- [Docs/agents/project-structure.md](Docs/agents/project-structure.md): architecture and source locations.
