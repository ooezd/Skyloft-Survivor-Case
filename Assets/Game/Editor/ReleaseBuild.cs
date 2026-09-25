using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class ReleaseBuild
{
    public const string PackageId = "com.ooezd.skyloft";
    private static bool queued;
    private static double queuedAfter;
    public static string LastResult { get; private set; } = "not-started";

    [MenuItem("Skyloft/Build/Clean Release Android APK")]
    public static void Queue()
    {
        if (queued || BuildPipeline.isBuildingPlayer)
            throw new InvalidOperationException("A build is already queued or running.");
        queued = true;
        queuedAfter = EditorApplication.timeSinceStartup + 5.0;
        LastResult = "queued";
        EditorApplication.update += RunQueued;
    }

    private static void RunQueued()
    {
        if (EditorApplication.timeSinceStartup < queuedAfter) return;
        EditorApplication.update -= RunQueued;
        queued = false;
        try { Build(); }
        catch (Exception error)
        {
            LastResult = "Failed: " + error.Message;
            Debug.LogException(error);
        }
    }

    private static void Build()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling)
            throw new InvalidOperationException("Stop Play Mode and wait for compilation before building.");
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            throw new InvalidOperationException("Select Android before building.");
        if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) != PackageId ||
            PlayerSettings.productName != "Survivinho")
            throw new InvalidOperationException("Restore the normal package ID and product name first.");
        if (EditorUserBuildSettings.buildAppBundle)
            throw new InvalidOperationException("Select APK output instead of an app bundle.");
        if (PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android).Contains("SKYLOFT_BENCHMARK"))
            throw new InvalidOperationException("Remove the benchmark define from Player Settings.");
        if (SessionState.GetBool(BenchmarkBuild.SessionKey, false))
            throw new InvalidOperationException("A benchmark scene injection session is still active.");

        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string output = Path.Combine(root, "Builds", "Release", "Skyloft-Survivor.apk");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        LastResult = "building";
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Game/Scenes/Game.unity" },
            locationPathName = output,
            target = BuildTarget.Android,
            options = BuildOptions.CleanBuildCache | BuildOptions.DetailedBuildReport,
            extraScriptingDefines = Array.Empty<string>()
        });
        LastResult = report.summary.result + "; errors=" + report.summary.totalErrors + "; " + output;
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(output), "build.txt"),
            "UTC: " + DateTime.UtcNow.ToString("O") + "\nUnity: " + Application.unityVersion +
            "\nPackage: " + PackageId + "\nDevelopment: false; CleanBuildCache: true" +
            "\nBenchmark define: absent\n" + LastResult);
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException(LastResult);
        Debug.Log("RELEASE BUILD " + LastResult);
    }
}
