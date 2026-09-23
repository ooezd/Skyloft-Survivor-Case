using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BenchmarkBuild
{
    public const string PackageId = "com.ooezd.skyloft.benchmark";
    internal const string SessionKey = "Skyloft.Benchmark.Building";
    private const string FingerprintKey = "Skyloft.Benchmark.Fingerprint";
    private const string RevisionKey = "Skyloft.Benchmark.Revision";
    private const string InjectedKey = "Skyloft.Benchmark.Injected";
    public static string LastResult { get; private set; } = "not-started";
    private static string queuedRevision;
    private static double queuedAfter;

    [MenuItem("Skyloft/Benchmark/Build Android APK")]
    public static void BuildMenu() => Queue("working-tree");

    // Return to the MCP caller before the synchronous Unity build blocks the main thread.
    public static void Queue(string revision)
    {
        if (queuedRevision != null || BuildPipeline.isBuildingPlayer)
            throw new InvalidOperationException("A build is already queued or running.");
        queuedRevision = revision;
        // Allow the MCP response to leave the main-thread dispatcher before blocking it.
        queuedAfter = EditorApplication.timeSinceStartup + 5.0;
        LastResult = "queued";
        EditorApplication.update += RunQueued;
    }

    private static void RunQueued()
    {
        if (EditorApplication.timeSinceStartup < queuedAfter) return;
        EditorApplication.update -= RunQueued;
        string revision = queuedRevision;
        queuedRevision = null;
        try { Build(revision); }
        catch (Exception error)
        {
            LastResult = "Failed: " + error.Message;
            Debug.LogException(error);
        }
    }

    // Called from the menu or MCP. Scene injection only affects the build copy.
    public static void Build(string revision)
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling)
            throw new InvalidOperationException("Stop Play Mode and wait for compilation before building.");
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            throw new InvalidOperationException("Select Android before building the benchmark.");
        var match = AssetDatabase.LoadAssetAtPath<MatchConfig>("Assets/Game/Settings/Match Settings.asset");
        if (match == null || Mathf.Abs(match.Duration - 180f) > 0.001f || match.Difficulties.Count != 3)
            throw new InvalidOperationException("Restore the standard 180-second match and three difficulties.");
        Debug.Log(WaveSystemChecks.Run(match));
        string oldId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
        string oldName = PlayerSettings.productName;
        bool oldBundle = EditorUserBuildSettings.buildAppBundle;
        UnityEngine.Object[] oldPreloaded = PlayerSettings.GetPreloadedAssets();
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string output = Path.Combine(root, "Builds", "Benchmark", "Skyloft-Benchmark.apk");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        string fingerprint = ComputeSourceFingerprint(root);
        SessionState.SetString(FingerprintKey, fingerprint);
        SessionState.SetString(RevisionKey, revision);
        SessionState.SetBool(SessionKey, true);
        SessionState.SetBool(InjectedKey, false);
        LastResult = "building";
        try
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageId);
            PlayerSettings.productName = "Survivinho Benchmark";
            EditorUserBuildSettings.buildAppBundle = false;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Game/Scenes/Game.unity" },
                locationPathName = output, target = BuildTarget.Android,
                // Incremental scene caching can reuse the previous injected identity, or leak
                // an injected scene into a later normal build. Reprocess benchmark scene data.
                options = BuildOptions.Development | BuildOptions.DetailedBuildReport | BuildOptions.CleanBuildCache,
                extraScriptingDefines = new[] { "SKYLOFT_BENCHMARK" }
            });
            LastResult = report.summary.result + "; errors=" + report.summary.totalErrors + "; " + output;
            if (report.summary.result == BuildResult.Succeeded && !SessionState.GetBool(InjectedKey, false))
                throw new InvalidOperationException("Benchmark scene injection did not run; do not use this APK.");
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(output), "build.txt"),
                "Protocol: " + BenchmarkRunner.Protocol + "\nRevision: " + revision +
                "\nSource SHA256: " + fingerprint + "\nUTC: " + DateTime.UtcNow.ToString("O") +
                "\nUnity: " + Application.unityVersion + "\nPackage: " + PackageId +
                "\nDevelopment: true; Deep Profiling: false; Autoconnect: false\n" + LastResult);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException(LastResult);
            Debug.Log("BENCHMARK BUILD " + LastResult);
        }
        finally
        {
            SessionState.SetBool(SessionKey, false);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, oldId);
            PlayerSettings.productName = oldName;
            EditorUserBuildSettings.buildAppBundle = oldBundle;
            PlayerSettings.SetPreloadedAssets(oldPreloaded);
            // BuildPipeline flushes temporary settings to disk; flush the restoration too.
            AssetDatabase.SaveAssets();
        }
    }

    // Includes content and relative paths, not timestamps or machine paths.
    // Hash is computed before temporary product/package overrides.
    public static string ComputeSourceFingerprint(string root)
    {
        var files = new[] { "Assets", "Packages", "ProjectSettings" }
            .SelectMany(folder => Directory.GetFiles(Path.Combine(root, folder), "*", SearchOption.AllDirectories))
            .OrderBy(path => path, StringComparer.Ordinal).ToArray();
        using (var hash = SHA256.Create())
        {
            byte[] buffer = new byte[65536];
            foreach (string path in files)
            {
                byte[] name = System.Text.Encoding.UTF8.GetBytes(path.Substring(root.Length).Replace('\\', '/') + "\n");
                hash.TransformBlock(name, 0, name.Length, null, 0);
                using (var stream = File.OpenRead(path))
                {
                    int read;
                    while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                        hash.TransformBlock(buffer, 0, read, null, 0);
                }
            }
            hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return BitConverter.ToString(hash.Hash).Replace("-", "").ToLowerInvariant();
        }
    }

    [MenuItem("Skyloft/Benchmark/Start In Play Mode")]
    public static void StartInPlayMode()
    {
        if (!EditorApplication.isPlaying || UnityEngine.Object.FindAnyObjectByType<BenchmarkRunner>() != null)
            throw new InvalidOperationException("Enter Play Mode in the Game scene's difficulty menu first.");
        new GameObject("Benchmark Runner").AddComponent<BenchmarkRunner>();
    }

    internal static void Inject(Scene scene)
    {
        if (!SessionState.GetBool(SessionKey, false)) return;
        if (scene.path != "Assets/Game/Scenes/Game.unity") return;
        var runner = new GameObject("Benchmark Runner").AddComponent<BenchmarkRunner>();
        SceneManager.MoveGameObjectToScene(runner.gameObject, scene);
        runner.SetBuildIdentity(SessionState.GetString(RevisionKey, "working-tree"),
            SessionState.GetString(FingerprintKey, "unknown"));
        SessionState.SetBool(InjectedKey, true);
    }
}

public sealed class BenchmarkSceneProcessor : IProcessSceneWithReport
{
    public int callbackOrder => 0;
    public void OnProcessScene(Scene scene, BuildReport report)
    {
        if (report != null) BenchmarkBuild.Inject(scene);
    }
}
