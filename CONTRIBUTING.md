# Contributor guide

## Pull requests

**Pull requests are welcome!**  
Please include a clear description of the changes you have made with your request; the title should follow
the [conventional commits](https://www.conventionalcommits.org/en/v1.0.0/) guideline.
All code should be covered by unit tests and comply with the coding guideline in this project.

### Technical expectations

As a framework for supporting unit testing, this project has a high standard for testing itself.  
In order to support this, static code analysis is performed
using [SonarCloud](https://sonarcloud.io/project/overview?id=Testably_aweXpect.Json) with quality gate requiring to

- solve all issues reported by SonarCloud
- have a code coverage of > 90%

## Building and testing

`dotnet build aweXpect.Json.slnx` builds everything; `./build.ps1 UnitTests` (or `./build.sh UnitTests`) runs the
unit tests like the CI, and `./build.ps1 ApiChecks` the public API tests.

All tests run on [TUnit](https://tunit.dev) and the Microsoft.Testing.Platform, which `global.json` selects for
`dotnet test`. Pass the project with `--project` and filter with `--treenode-filter`:

```shell
dotnet test
dotnet test --project Tests/aweXpect.Json.Tests -f net8.0
dotnet test --project Tests/aweXpect.Json.Tests -f net8.0 --treenode-filter "/*/*/ThatJsonElement+IsNull+Tests/*"
```

The public API tests in `Tests/aweXpect.Json.Api.Tests` compare the public API with the snapshots next to them. To
accept an intentional change, run the explicit test `AcceptApiChanges` on purpose and commit the updated snapshots:

```shell
dotnet test --project Tests/aweXpect.Json.Api.Tests --treenode-filter "/*/*/ApiApprovalTests/AcceptApiChanges"
```

A normal run skips `AcceptApiChanges`, because it is `[Explicit]`; TUnit only runs it when every test that the filter
selects is explicit.
