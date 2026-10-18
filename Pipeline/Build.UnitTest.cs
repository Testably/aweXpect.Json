using System;
using System.Linq;
using Fallout.Common;
using Fallout.Solutions;
using Fallout.Common.Tooling;
using Fallout.Common.Tools.DotNet;
using static Fallout.Common.Tools.DotNet.DotNetTasks;

// ReSharper disable AllUnderscoreLocalParameterName

namespace Build;

partial class Build
{
	const string NetFramework = "net48";

	Target UnitTests => _ => _
		.DependsOn(DotNetFrameworkUnitTests)
		.DependsOn(DotNetUnitTests);

	Project[] UnitTestProjects =>
	[
		Solution.Tests.aweXpect_Json_Tests,
	];

	Target DotNetFrameworkUnitTests => _ => _
		.Unlisted()
		.DependsOn(Compile)
		.OnlyWhenDynamic(() => EnvironmentInfo.IsWin)
		.Executes(() =>
		{
			RunUnitTests(framework => framework == NetFramework);
		});

	Target DotNetUnitTests => _ => _
		.Unlisted()
		.DependsOn(Compile)
		.Executes(() =>
		{
			RunUnitTests(framework => framework != NetFramework);
		});

	/// <summary>
	///     Runs the unit tests for their matching target frameworks, each writing a TRX file and a Cobertura coverage
	///     file named after the project and the target framework into the <see cref="TestResultsDirectory" />.
	/// </summary>
	/// <remarks>
	///     <c>global.json</c> opts into the Microsoft.Testing.Platform, so <c>dotnet test</c> takes the project with
	///     <c>--project</c> instead of as a positional argument.
	/// </remarks>
	void RunUnitTests(Func<string, bool> includeFramework)
	{
		(Project Project, string Framework)[] testRuns = UnitTestProjects
			.SelectMany(project => (project.GetTargetFrameworks() ?? [])
				.Where(includeFramework)
				.Select(framework => (project, framework)))
			.ToArray();

		Assert.NotEmpty(testRuns);

		DotNetTest(s => s
				.SetConfiguration(Configuration)
				.SetProcessEnvironmentVariable("DOTNET_CLI_UI_LANGUAGE", "en-US")
				.EnableNoBuild()
				.SetResultsDirectory(TestResultsDirectory)
				.CombineWith(testRuns,
					(settings, testRun) => settings
						.SetFramework(testRun.Framework)
						.AddProcessAdditionalArguments(
							$"--project \"{testRun.Project.Path}\"",
							"--report-trx",
							$"--report-trx-filename {testRun.Project.Name}_{testRun.Framework}.trx",
							"--coverage",
							"--coverage-output-format cobertura",
							$"--coverage-output {testRun.Project.Name}_{testRun.Framework}.cobertura.xml")),
			completeOnFailure: true);
	}
}
