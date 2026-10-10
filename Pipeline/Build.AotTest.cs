using System.Runtime.InteropServices;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Tooling;
using Fallout.Common.Tools.DotNet;
using Fallout.Solutions;
using static Fallout.Common.Tools.DotNet.DotNetTasks;

// ReSharper disable UnusedMember.Local
// ReSharper disable AllUnderscoreLocalParameterName

namespace Build;

partial class Build
{
	/// <summary>
	///     Runs the smoke tests from the compiled output and again published with Native AOT for the current platform.
	/// </summary>
	/// <remarks>
	///     The trim analyzer checks annotations, not behaviour, so this is the only place that verifies that an
	///     expectation returns the same result in a trimmed application as under the JIT.
	/// </remarks>
	Target AotSmokeTests => _ => _
		.DependsOn(Compile)
		.Executes(() =>
		{
			Project project = Solution.Tests.Aot.aweXpect_Json_Aot;
			DotNetRun(s => s
				.SetProjectFile(project)
				.SetConfiguration(Configuration)
				.EnableNoBuild());

			AbsolutePath output = ArtifactsDirectory / "Aot" / project.Name;
			DotNetPublish(s => s
				.SetProject(project)
				.SetConfiguration(Configuration)
				.SetRuntime(RuntimeInformation.RuntimeIdentifier)
				.SetOutput(output));

			string executable = output / (project.Name + (EnvironmentInfo.IsWin ? ".exe" : ""));
			ProcessTasks.StartProcess(executable, string.Empty, output)
				.AssertZeroExitCode();
		});
}
