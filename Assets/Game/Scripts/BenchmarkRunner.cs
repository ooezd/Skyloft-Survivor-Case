#if UNITY_EDITOR || SKYLOFT_BENCHMARK
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using Unity.Profiling;
using UnityEngine;

// Included only by the benchmark build processor (or explicitly in an Editor test).
// The live Game scene, prefabs, and normal APK contain no runner.
[DefaultExecutionOrder(-200)]
public sealed class BenchmarkRunner : MonoBehaviour
{
    public const string Protocol = "hard-route-v1";
    public const int Seed = 230926;
    public const float WarmupSeconds = 15f;
    public const int FrameLimit = 65536;
    [SerializeField] private string sourceFingerprint = "editor-working-tree";
    [SerializeField] private string sourceRevision = "working-tree";
    public bool Completed { get; private set; }
    public string OutputDirectory { get; private set; }
    public int SampleCount => count;

    private struct Frame
    {
        public double wallSeconds;
        public float gameSeconds, frameMs, x, z;
        public int enemies, kills, spawns;
        public long gcBytes, mainThreadNs, memoryBytes, drawCalls, triangles;
    }

    [Serializable]
    private sealed class Report
    {
        public string protocol, revision, fingerprint, utc, status, device, os, unity, gpu, graphicsApi;
        public string quality, resolution, note, difficulty, wavePlan;
        public int seed, targetFps, frames, measuredFrames, kills, spawns, peakEnemies, enemyCap;
        public bool editor, development, gcAvailable, mainThreadAvailable, renderCountersAvailable;
        public float warmupSeconds, meanFrameMs, medianFrameMs, p95FrameMs, p99FrameMs, maxFrameMs;
        public double meanEnemies, wallDuration;
        public long peakMemoryBytes;
    }

    private Frame[] frames;
    private int count;
    private GameManager manager;
    private PlayerController player;
    private EnemySpawner spawner;
    private Health playerHealth;
    private ProfilerRecorder gc, mainThread, memory, drawCalls, triangles;
    private bool running;
    private double startWall, previousWall;
    private int previousFps, previousVsync, previousSleep;
    private Report report;

    public void SetBuildIdentity(string revision, string fingerprint)
    {
        sourceRevision = revision;
        sourceFingerprint = fingerprint;
    }

    // Three seconds moving, three seconds standing on each side of a square.
    // Uses the existing PlayerController movement, not a second movement simulation.
    public static Vector2 InputAt(float elapsed)
    {
        float cycle = Mathf.Repeat(Mathf.Max(0f, elapsed), 24f);
        if (cycle % 6f >= 3f) return Vector2.zero;
        switch ((int)(cycle / 6f))
        {
            case 0: return Vector2.right;
            case 1: return Vector2.up;
            case 2: return Vector2.left;
            default: return Vector2.down;
        }
    }

    private IEnumerator Start()
    {
        // GameManager.Start must initialize its normal menu state first.
        yield return null;
        manager = FindAnyObjectByType<GameManager>();
        player = FindAnyObjectByType<PlayerController>();
        spawner = FindAnyObjectByType<EnemySpawner>();
        if (manager == null || player == null || spawner == null ||
            manager.State != GameManager.RunState.SelectingDifficulty)
        {
            Debug.LogError("Benchmark requires the untouched Game scene in its difficulty menu.");
            enabled = false;
            yield break;
        }

        frames = new Frame[FrameLimit]; // Allocate once, before the measured run.
        previousFps = Application.targetFrameRate;
        previousVsync = QualitySettings.vSyncCount;
        previousSleep = Screen.sleepTimeout;
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
        mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1);
        memory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory", 1);
        drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", 1);
        triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count", 1);
        report = new Report
        {
            protocol = Protocol, revision = sourceRevision, fingerprint = sourceFingerprint, status = "started",
            utc = DateTime.UtcNow.ToString("O"), device = SystemInfo.deviceModel,
            os = SystemInfo.operatingSystem, unity = Application.unityVersion,
            gpu = SystemInfo.graphicsDeviceName, graphicsApi = SystemInfo.graphicsDeviceType.ToString(),
            quality = QualitySettings.names[QualitySettings.GetQualityLevel()],
            resolution = Screen.width + "x" + Screen.height, seed = Seed,
            targetFps = Application.targetFrameRate, editor = Application.isEditor,
            development = Debug.isDebugBuild, warmupSeconds = WarmupSeconds,
            gcAvailable = gc.Valid, mainThreadAvailable = mainThread.Valid,
            renderCountersAvailable = drawCalls.Valid && triangles.Valid, peakMemoryBytes = -1,
            note = "Repeatable inputs, NOT lockstep determinism. First 15 gameplay seconds excluded from summary, retained in CSV. Player cannot die; upgrades off. Main Thread includes waits; it is not CPU busy time. Counter samples refer to completed frames and may lag gameplay columns. -1 means unavailable. No GPU timing collected."
        };
        OutputDirectory = Path.Combine(Application.persistentDataPath, "Benchmarks",
            DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
        Directory.CreateDirectory(OutputDirectory);
        playerHealth = player.GetComponent<Health>();
        playerHealth.BenchmarkPreventDeath = true;
        player.BenchmarkInput = Vector2.zero;
        spawner.SetBenchmarkSeed(Seed);
        manager.StartBenchmarkRun();
        if (manager.SelectedDifficulty != null)
        {
            report.difficulty = manager.SelectedDifficulty.DisplayName;
            report.enemyCap = manager.SelectedDifficulty.MaximumActiveEnemies;
            report.wavePlan = JsonUtility.ToJson(manager.SelectedDifficulty.WavePlan);
        }
        File.WriteAllText(Path.Combine(OutputDirectory, "started.json"), JsonUtility.ToJson(report, true));
        if (manager.State != GameManager.RunState.Playing)
        {
            Finish("invalid-start");
            yield break;
        }
        startWall = previousWall = Time.realtimeSinceStartupAsDouble;
        running = true;
        Debug.Log("BENCHMARK START " + Protocol + " -> " + OutputDirectory);
    }

    private static long Read(ProfilerRecorder recorder) =>
        recorder.Valid && recorder.Count > 0 ? recorder.LastValue : -1;

    private void Update()
    {
        if (!running) return;
        double now = Time.realtimeSinceStartupAsDouble;
        float elapsed = 180f - manager.RemainingTime;
        if (manager.State != GameManager.RunState.Playing)
        {
            // Exclude result-screen cleanup/serialization from performance samples.
            Finish(manager.State == GameManager.RunState.Won ? "completed" : "invalid-early-end");
            return;
        }
        if (count == frames.Length)
        {
            Finish("invalid-buffer-full");
            return;
        }
        Vector3 position = player.transform.position;
        frames[count++] = new Frame
        {
            wallSeconds = now - startWall, gameSeconds = elapsed,
            frameMs = (float)((now - previousWall) * 1000.0),
            enemies = spawner.ActiveEnemies.Count, kills = spawner.KillCount,
            spawns = spawner.BenchmarkSpawnCount, x = position.x, z = position.z,
            gcBytes = Read(gc), mainThreadNs = Read(mainThread), memoryBytes = Read(memory),
            drawCalls = Read(drawCalls), triangles = Read(triangles)
        };
        previousWall = now;
        player.BenchmarkInput = InputAt(elapsed);
    }

    public static float Percentile(float[] sorted, float fraction) => sorted.Length == 0 ? 0f :
        sorted[Mathf.Clamp(Mathf.CeilToInt(sorted.Length * fraction) - 1, 0, sorted.Length - 1)];

    private void Finish(string status)
    {
        if (Completed || report == null) return;
        running = false;
        Completed = true;
        report.status = status;
        report.frames = count;
        report.wallDuration = count > 0 ? frames[count - 1].wallSeconds : 0;
        report.kills = spawner.KillCount;
        report.spawns = spawner.BenchmarkSpawnCount;
        int measured = 0;
        for (int i = 0; i < count; i++)
            if (frames[i].gameSeconds >= WarmupSeconds) measured++;
        var times = new float[measured];
        double sum = 0, enemyTime = 0, sampledSeconds = 0;
        int index = 0;
        for (int i = 0; i < count; i++)
        {
            Frame frame = frames[i];
            if (frame.gameSeconds < WarmupSeconds) continue;
            times[index++] = frame.frameMs;
            sum += frame.frameMs;
            enemyTime += frame.enemies * frame.frameMs;
            sampledSeconds += frame.frameMs;
            report.peakEnemies = Mathf.Max(report.peakEnemies, frame.enemies);
            report.peakMemoryBytes = Math.Max(report.peakMemoryBytes, frame.memoryBytes);
        }
        Array.Sort(times);
        report.measuredFrames = measured;
        report.meanFrameMs = measured > 0 ? (float)(sum / measured) : 0;
        report.meanEnemies = sampledSeconds > 0 ? enemyTime / sampledSeconds : 0;
        report.medianFrameMs = Percentile(times, 0.5f);
        report.p95FrameMs = Percentile(times, 0.95f);
        report.p99FrameMs = Percentile(times, 0.99f);
        report.maxFrameMs = measured > 0 ? times[measured - 1] : 0;
        DisposeRecorders();
        RestoreSettings();
        try
        {
            using (var writer = new StreamWriter(Path.Combine(OutputDirectory, "frames.csv")))
            {
                writer.WriteLine("wall_seconds,game_seconds,frame_ms,active_enemies,kills,spawned,player_x,player_z,gc_bytes,main_thread_ns,total_used_memory_bytes,draw_calls,triangles");
                for (int i = 0; i < count; i++)
                {
                    Frame f = frames[i];
                    writer.WriteLine(string.Format(CultureInfo.InvariantCulture,
                        "{0:F6},{1:F6},{2:F6},{3},{4},{5},{6:F5},{7:F5},{8},{9},{10},{11},{12}",
                        f.wallSeconds, f.gameSeconds, f.frameMs, f.enemies, f.kills, f.spawns,
                        f.x, f.z, f.gcBytes, f.mainThreadNs, f.memoryBytes, f.drawCalls, f.triangles));
                }
            }
            File.WriteAllText(Path.Combine(OutputDirectory, "summary.json"), JsonUtility.ToJson(report, true));
            Debug.Log("BENCHMARK " + status + " -> " + OutputDirectory);
        }
        catch (Exception error) { Debug.LogException(error); }
    }

    private void RestoreSettings()
    {
        if (player != null) player.BenchmarkInput = null;
        if (playerHealth != null) playerHealth.BenchmarkPreventDeath = false;
        Application.targetFrameRate = previousFps;
        QualitySettings.vSyncCount = previousVsync;
        Screen.sleepTimeout = previousSleep;
    }

    private void DisposeRecorders()
    {
        gc.Dispose(); mainThread.Dispose(); memory.Dispose(); drawCalls.Dispose(); triangles.Dispose();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && running) Finish("invalid-paused");
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused && running && !Application.isEditor) Finish("invalid-focus-lost");
    }

    private void OnDestroy()
    {
        if (running) Finish("invalid-interrupted");
        else DisposeRecorders();
    }
}
#endif
