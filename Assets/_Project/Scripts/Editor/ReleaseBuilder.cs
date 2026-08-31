using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BankShot.EditorTools
{
    /// <summary>Build standalone Windows (per i playtest del Gate 1 e oltre).</summary>
    public static class ReleaseBuilder
    {
        const string OutputPath = "Builds/Windows/BANK SHOOT.exe";

        [MenuItem("BankShot/Build Windows")]
        public static void BuildWindows()
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            BuildReport report = BuildPipeline.BuildPlayer(scenes, OutputPath,
                BuildTarget.StandaloneWindows64, BuildOptions.None);

            if (report.summary.result != BuildResult.Succeeded)
                throw new System.InvalidOperationException(
                    $"Build fallita: {report.summary.result}, {report.summary.totalErrors} errori");

            Debug.Log($"[ReleaseBuilder] Build ok: {report.summary.outputPath} " +
                      $"({report.summary.totalSize / (1024 * 1024)} MB)");
        }

        /// <summary>Per il batchmode: rigenera la scena e poi builda.</summary>
        public static void BuildAll()
        {
            SandboxSceneBuilder.Build();
            BuildWindows();
        }
    }
}
