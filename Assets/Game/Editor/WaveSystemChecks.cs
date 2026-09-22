using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Repeatable deterministic checks, without entering Play Mode or modifying saved assets.
public static class WaveSystemChecks
{
    [MenuItem("Skyloft/Validation/Check Wave System")]
    public static void RunMenu() => Debug.Log(Run());

    public static string Run()
    {
        int checks = 0;
        Action<bool, string> check = (ok, message) => { if (!ok) throw new Exception(message); checks++; };
        foreach (string level in new[] { "Easy", "Normal", "Hard" })
        {
            var profile = AssetDatabase.LoadAssetAtPath<DifficultyConfig>($"Assets/Game/Settings/{level}.asset");
            check(profile != null && profile.WavePlan != null, level + " asset references");
            check(profile.WavePlan.ValidateForMatch(180f, out _) && profile.WavePlan.WaveCount == 6, level + " valid schedule");
            var director = new WaveDirector();
            int total = 0;
            director.Begin(profile.WavePlan);
            for (int w = 0; w < 6; w++)
            {
                var wave = profile.WavePlan.GetWave(w);
                for (int n = 0; n < wave.enemyCount; n++)
                    director.Tick(w * 30f + n * (wave.spawnInterval + 0.001f), () => { total++; return true; });
                check(director.WaveNumber == w + 1 && director.SpawnedThisWave == wave.enemyCount && director.State == WaveDirector.WaveState.WaitingForNextWave, level + " wave quota " + w);
                int before = total;
                director.Tick(w * 30f + 29.9f, () => { total++; return true; });
                check(total == before, "Rest period must not spawn");
            }
            director.Tick(180f, () => throw new Exception("Spawn after schedule"));
            check(director.State == WaveDirector.WaveState.Completed, "Schedule completion");
        }
        var plan = AssetDatabase.LoadAssetAtPath<WavePlanConfig>("Assets/Game/Settings/Waves/Easy Waves.asset");
        var blocked = new WaveDirector();
        blocked.Begin(plan);
        blocked.Tick(0f, () => false);
        check(blocked.SpawnedThisWave == 0, "Full capacity consumes no quota");
        blocked.Tick(0.1f, () => throw new Exception("Retry before interval"));
        blocked.Tick(1.3f, () => true);
        check(blocked.SpawnedThisWave == 1, "Capacity recovery");
        blocked.Tick(90f, () => true);
        check(blocked.WaveNumber == 4 && blocked.SpawnedThisWave == 1, "Long frame skips expired quotas");
        blocked.Stop();
        blocked.Tick(120f, () => throw new Exception("Spawn after stop"));
        check(blocked.State == WaveDirector.WaveState.Stopped, "Stop");
        blocked.Begin(plan);
        check(blocked.WaveNumber == 1 && blocked.SpawnedThisWave == 0, "Restart resets state");
        var invalid = ScriptableObject.CreateInstance<WavePlanConfig>();
        try
        {
            check(!invalid.ValidateForMatch(180f, out _), "Empty schedule rejected");
            var so = new SerializedObject(invalid);
            var waves = so.FindProperty("waves");
            waves.arraySize = 1;
            var wave = waves.GetArrayElementAtIndex(0);
            wave.FindPropertyRelative("duration").floatValue = 30f;
            wave.FindPropertyRelative("enemyCount").intValue = 1;
            wave.FindPropertyRelative("spawnInterval").floatValue = 1f;
            so.ApplyModifiedPropertiesWithoutUndo();
            check(!invalid.ValidateForMatch(180f, out _), "Short schedule rejected");
            wave.FindPropertyRelative("duration").floatValue = 180f;
            wave.FindPropertyRelative("spawnInterval").floatValue = 0f;
            so.ApplyModifiedPropertiesWithoutUndo();
            check(!invalid.ValidateForMatch(180f, out _), "Invalid interval rejected");
        }
        finally { UnityEngine.Object.DestroyImmediate(invalid); }
        return $"Wave system: {checks} checks passed.";
    }
}
