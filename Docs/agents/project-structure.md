# Project Structure

## Repository layout

| Path | Contains |
|---|---|
| `Source/aweXpect.Json/` | The library and the only shipped package. |
| `Tests/aweXpect.Json.Tests/` | Unit tests (TUnit). |
| `Tests/Shared/` | Helpers compiled into the test projects (`Tests/Directory.Build.props`). |
| `Tests/aweXpect.Json.Api.Tests/` | Public API snapshot tests (TUnit). |
| `Tests/Aot/aweXpect.Json.Aot/` | Native AOT smoke tests as a console application. |
| `Benchmarks/aweXpect.Json.Benchmarks/` | BenchmarkDotNet benchmarks. |
| `Pipeline/` | The Fallout build pipeline, run through `build.ps1`/`build.sh`. |
| `Docs/pages/` | User documentation, published to the docs site. |
| `Docs/agents/` | Guides for authors and agents, not published. |

Shared MSBuild settings live in `Directory.Build.props` (strong naming), `Source/Directory.Build.props` (target frameworks, package metadata, warnings as errors, trimming and AOT flags), `Tests/Directory.Build.props` (TUnit, shared by the unit and API tests) and `Tests/Aot/Directory.Build.props`.

## Library

The root namespace is `aweXpect`, so the expectations show up next to the built-in ones.

- `ThatJsonString.*.cs`: expectations on `string` (`IsValidJson`, `IsValidJsonMatching`, `IsValidJsonMatchingExactly`).
- `JsonExtensions.cs`: `AsJson()` on the string equality result, which plugs `JsonMatchType` into `IsEqualTo(...)`.
- `ThatJsonElement.*.cs`, `ThatNullableJsonElement.*.cs`, `ThatJsonNode.*.cs`: expectations on `JsonElement`, `JsonElement?` and `JsonNode` (`Matches`, `MatchesExactly`, `IsObject`, `IsArray`, `IsString`, `IsNumber`, `IsBoolean`, `IsNull`). Each class is a `static partial class` with one file per expectation; the file without a suffix holds the shared constraints.
- `ThatJsonObject.*.cs`: expectations on any object (`IsJsonSerializable`, `IsJsonSerializedAs`).
- `Json/` (namespace `aweXpect.Json`):
  - `JsonOptions`, `JsonOptionsExtensions`, `JsonNumbersWithinOptions`, `JsonNumberTolerance`: the comparison options such as `IgnoringArrayOrder()`, `IgnoringMember(...)` and `ComparingNumbersWithin(...)`, listed in the "JSON options" context of a failure.
  - `JsonElementValidator`: compares two `JsonElement`s and reports each difference by its JSON path (`JsonPathSegment`).
  - `ExpectationJsonConverter`: serializes the expected value and the `It.Is...` expectations inside it; reflection-based, so guarded by `ReflectionFallback.IsSupported`.
  - `IJsonObjectResult`, `IJsonArrayResult`, `IJsonPropertyResult`: the fluent builders behind `IsObject(o => ...)` and `IsArray(a => ...)`.
  - `JsonAwexpectCustomizationExtensions`: `Customize.aweXpect.Json()` with `DefaultJsonDocumentOptions` and `DefaultJsonSerializerOptions`.
  - `JsonValidation`, `JsonMatchType`, `MembersToIgnoreTracker`, `NamedMemberToIgnore`: internal helpers.
- `Results/`: result types such as `IsArrayResult` (`WhoseElements`) and `JsonWhichResult`.
- `Formatting/JsonFormatting.cs`: formatting of `JsonElement` values in failure messages.
- `Helpers/`: `ThrowHelper`, `ReflectionFallbackHelpers` and polyfills for `netstandard2.0`.

## Tests

- Unit test files mirror the source files: `ThatJsonElement.IsNull.cs` is tested in `ThatJsonElement.IsNull.Tests.cs` as the nested class `ThatJsonElement.IsNull.Tests`. Shared helpers such as `ThatJsonElement.FromString(...)` sit in the file without a suffix. Option and customization tests are in `Json/`.
- Failure tests assert `Throws<FailException>().WithMessage(...)` with the full message as a raw string literal; aweXpect throws its own `FailException` under TUnit.
- `Tests/aweXpect.Json.Api.Tests/Helper.cs` locates the library build per target framework by searching for `aweXpect.Json.slnx` above the output directory.
- `Tests/Aot/aweXpect.Json.Aot/Checks.cs` lists the smoke checks; each states the result that must hold both under the JIT and published with Native AOT, including the failure when the reflection fallback is switched off.

## Notes for changes

- A new expectation usually needs the source file per subject type, unit tests, an AOT check if it serializes or reflects, the updated API snapshots, and the README table and docs page.
- Code compiled for `netstandard2.0` references the `System.Text.Json` package; on `net8.0` and `net10.0` the framework version is used, so behaviour can differ between target frameworks.
- The trim and AOT analyzers are disabled for `net8.0` because they predate feature guards; the `net10.0` build is the one that reports unguarded reflection.
