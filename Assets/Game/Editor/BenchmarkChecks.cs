using System;
using UnityEditor;
using UnityEngine;

public static class BenchmarkChecks
{
    [MenuItem("Skyloft/Validation/Check Benchmark Protocol")]
    public static void RunMenu() => Debug.Log(Run());

    public static string Run()
    {
        int checks = 0;
        Action<bool, string> check = (ok, message) =>
        {
            if (!ok) throw new Exception(message);
            checks++;
        };
        check(BenchmarkRunner.InputAt(0f) == Vector2.right, "Route starts right");
        check(BenchmarkRunner.InputAt(3f) == Vector2.zero, "Pause boundary");
        check(BenchmarkRunner.InputAt(6f) == Vector2.up, "Second leg");
        check(BenchmarkRunner.InputAt(12f) == Vector2.left, "Third leg");
        check(BenchmarkRunner.InputAt(18f) == Vector2.down, "Fourth leg");
        check(BenchmarkRunner.InputAt(24f) == Vector2.right, "Cycle boundary");
        Vector2 sum = Vector2.zero;
        for (int i = 0; i < 240; i++) sum += BenchmarkRunner.InputAt(i * 0.1f);
        check(sum.sqrMagnitude < 0.001f, "Ideal route closes after one cycle");
        check(BenchmarkRunner.Percentile(new[] { 1f, 2f, 3f, 4f }, 0.95f) == 4f, "Nearest-rank p95");
        check(BenchmarkRunner.Percentile(Array.Empty<float>(), 0.5f) == 0f, "Empty samples");
        var fixture = new GameObject("Benchmark health check");
        try
        {
            var health = fixture.AddComponent<Health>();
            // Edit Mode doesn't run Awake; initialize through the serialized value.
            var serialized = new SerializedObject(health);
            serialized.FindProperty("currentHealth").intValue = 100;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            int deaths = 0, hits = 0;
            health.Died += _ => deaths++;
            health.DamageReceived += (_, __) => hits++;
            health.BenchmarkPreventDeath = true;
            health.TakeDamage(200);
            check(health.CurrentHealth == 1 && deaths == 0 && hits == 1, "Benchmark preserves feedback without death");
            health.BenchmarkPreventDeath = false;
            health.TakeDamage(1);
            check(!health.IsAlive && deaths == 1 && hits == 2, "Normal death still works");
        }
        finally { UnityEngine.Object.DestroyImmediate(fixture); }
        return "Benchmark protocol: " + checks + " checks passed.";
    }
}
