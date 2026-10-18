# Workflows

Commands run from the repository root. Examples use PowerShell; `./build.sh` takes the same arguments as `./build.ps1` on Linux and macOS.

## Build

```powershell
dotnet build aweXpect.Json.slnx
```

The SDK comes from `global.json`. CI also installs the 8.0 and 9.0 runtimes; install the .NET 8 runtime locally to run the `net8.0` tests. `GeneratePackageOnBuild` is on, so a build of `Source/aweXpect.Json` also writes a NuGet package to its `bin` folder.

## Unit tests

`Tests/aweXpect.Json.Tests` uses xUnit v2 and targets `net8.0`, `net10.0` and `net48` (`Tests/Directory.Build.props`). Filter by the nested class of the expectation:

```powershell
dotnet test Tests/aweXpect.Json.Tests -f net10.0 --filter "FullyQualifiedName~ThatJsonElement+IsNull"
dotnet test Tests/aweXpect.Json.Tests -f net48 --filter "FullyQualifiedName~ThatJsonElement+IsNull"
```

The test classes are nested as `ThatJsonElement` > `IsNull` > `Tests`, so the filter uses `+` between the outer classes. Use `--no-build` after a fresh `dotnet build`. Set `$env:DOTNET_CLI_UI_LANGUAGE = "en-US"` for English output on a localized machine.

## Public API tests

`Tests/aweXpect.Json.Api.Tests` (NUnit, `net8.0`) writes the public API of the `Source/aweXpect.Json` build for every target framework in `Source/Directory.Build.props` and compares it with the snapshots `ApiApprovalTests.aweXpect%2EJson.<framework>.awexpect-snapshot.txt` (`aweXpect.Snapshots.PublicApi`). The project builds all target frameworks of the library itself.

```powershell
dotnet test Tests/aweXpect.Json.Api.Tests
```

To accept an intentional change, run the explicit test `AcceptApiChanges`, or run the tests with the environment variable `AWEXPECT_ACCEPT_SNAPSHOTS` set to `all`, then review and commit the changed snapshot files. The tests read the `Debug` build when compiled in `Debug` and the `Release` build otherwise.

## AOT smoke tests

`Tests/Aot/aweXpect.Json.Aot` is a console application (`net10.0`, `PublishAot`) that runs every check in `Checks.cs` and returns a non-zero exit code when one fails. Trimmer and AOT compiler warnings fail the publish.

```powershell
dotnet run --project Tests/Aot/aweXpect.Json.Aot
./build.ps1 AotSmokeTests
```

The `dotnet run` variant only covers the JIT run. `AotSmokeTests` also publishes with Native AOT for the current platform and runs the executable, which needs the native toolchain (on Windows the C++ build tools of Visual Studio).

## Benchmarks

`Benchmarks/aweXpect.Json.Benchmarks` uses BenchmarkDotNet (`net8.0`, Release only).

```powershell
dotnet run -c Release --project Benchmarks/aweXpect.Json.Benchmarks -- --filter "*IsValidJson*"
./build.ps1 Benchmarks
```

`Benchmarks` runs all benchmarks and writes the results to `Artifacts/Benchmarks`. On a PR, CI posts the report as a comment.

## Mutation tests

`./build.ps1 MutationTests` installs `dotnet-stryker` into `TestResults/dotnet-stryker` and mutates `Source/aweXpect.Json` against `Tests/aweXpect.Json.Tests` on `net10.0` (mutation level `Advanced`). Off `main` it only mutates changes since `main`. The run is slow; leave it to CI unless a mutation report is needed. The report goes to `Artifacts/Stryker`.

## Documentation

- `Docs/pages/*.md` is published to [docs.testably.org](https://docs.testably.org/aweXpect/extensions/aweXpect.Json/). `notify-docs-site.yml` triggers a rebuild of the site after a successful `Build` run on `main`. `Docs/pages/00-index.md` embeds the README through `{README}`.
- `README.md` is also the package readme. Its overview table links into `Docs/pages`; keep both in sync when adding or renaming an expectation.
- `Docs/agents/` holds this guide and is not published.

## Build pipeline

`build.ps1`, `build.sh` and `build.cmd` build and run `Pipeline/Build.csproj`, a [Fallout](https://www.nuget.org/packages/Fallout.Common) build (`.fallout/parameters.json` names the solution). The configuration defaults to `Debug` locally and `Release` on a server. List the targets with `./build.ps1 --help`.

Always pass a target: without one, the pipeline runs the defaults `ApiChecks`, `Benchmarks` and `CodeAnalysis`.

| Target | Does |
|---|---|
| `Compile` | Cleans `Artifacts/` and `TestResults/`, restores, calculates the version with GitVersion and builds the solution. |
| `UnitTests` | Runs the unit tests on all .NET target frameworks with coverage, and the `net48` tests with the xUnit console runner on Windows only. |
| `CodeCoverage` | `UnitTests` plus an OpenCover report in `TestResults/reports`. |
| `ApiChecks` | Runs the public API tests. |
| `AotSmokeTests` | Runs the smoke tests, then publishes them with Native AOT and runs the executable. |
| `Benchmarks` | Runs the benchmarks (`BenchmarkDotNet`) and logs the report (`BenchmarkResult`). |
| `MutationTests` | Runs Stryker (`MutationTestExecution`) and prepares the PR comment (`MutationComment`). |
| `Pack` | Builds the package with a README whose badges point to the release (`UpdateReadme`) and moves it to `Artifacts/Packages`. |
| `CodeAnalysis` | SonarCloud analysis; needs `SonarToken`, used by CI only. |
| `BenchmarkComment`, `MutationTestDashboard` | Download the CI artifacts and post or upload the results; used by `ci-analysis.yml` only. |

`Build.cs` uses `[Solution(GenerateProjects = true)]`, so projects are referenced as `Solution.Tests.aweXpect_Json_Tests`; a new project in `aweXpect.Json.slnx` becomes available the same way.

## CI workflows

- `ci.yml` (pull requests to `main`): unit tests (`CodeCoverage`) and AOT smoke tests on Ubuntu, Windows and macOS, API tests, mutation tests, benchmarks, SonarCloud analysis (skipped for Dependabot and forks) and a test result summary.
- `ci-analysis.yml` (after `CI` completes): uploads the mutation report to the Stryker dashboard and comments mutation and benchmark results on the PR.
- `build.yml` (pushes to `main` and `v*` tags): the same checks plus `Pack`. On a tag, the packages are pushed to nuget.org after the test, API, AOT and analysis jobs passed, a GitHub release is created for a stable version, and the released issues and PRs are commented and labelled.
- `notify-docs-site.yml`: triggers the docs site rebuild.
- Dependabot updates NuGet packages and actions weekly with the `chore` prefix; Mergify squash-merges its PRs once the unit and API tests pass.
