using System;
using UnityEditor;
using UnityEngine;

// Checks use serialized match references; scheduling cases use temporary in-memory fixtures.
public static class WaveSystemChecks
{
    [MenuItem("Skyloft/Validation/Check Wave System")]
    public static void RunMenu()
    {
        var settings = Selection.activeObject as MatchConfig;
        if (settings == null)
        {
            var manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            if (manager != null)
                settings = new SerializedObject(manager).FindProperty("matchSettings").objectReferenceValue as MatchConfig;
        }
        Debug.Log(Run(settings));
    }

    public static string Run(MatchConfig settings)
    {
        int checks = 0;
        Action<bool, string> check = (ok, message) => { if (!ok) throw new Exception(message); checks++; };
        check(settings != null, "Select Match Settings or open a scene with a configured GameManager.");
        check(settings.Difficulties != null && settings.Difficulties.Count > 0, "Assign difficulty references.");
        foreach (var profile in settings.Difficulties)
        {
            check(profile != null && profile.WavePlan != null, "Difficulty and wave plan references are required.");
            check(profile.WavePlan.ValidateForMatch(settings.Duration, out string error), profile.DisplayName + ": " + error);
        }

        var plan = ScriptableObject.CreateInstance<WavePlanConfig>();
        try
        {
            check(!plan.Validate(out _), "Empty schedule rejected");
            var serialized = new SerializedObject(plan);
            var waves = serialized.FindProperty("waves");
            waves.arraySize = 3;
            float[] durations = { 4f, 7f, 3f };
            for (int i = 0; i < waves.arraySize; i++)
            {
                var wave = waves.GetArrayElementAtIndex(i);
                wave.FindPropertyRelative("duration").floatValue = durations[i];
                wave.FindPropertyRelative("enemyCount").intValue = 2;
                wave.FindPropertyRelative("spawnInterval").floatValue = 1f;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            plan.BackgroundWave.spawnInterval = 2f;
            check(plan.ValidateForMatch(plan.TotalDuration + 5f, out _), "Constant wave covers time after schedule");
            plan.BackgroundWave.enabled = false;
            check(!plan.ValidateForMatch(plan.TotalDuration + 5f, out _), "Short schedule without constant wave rejected");
            var director = new WaveDirector();
            director.Begin(plan);
            float start = 0f;
            for (int i = 0; i < plan.WaveCount; i++)
            {
                var wave = plan.GetWave(i);
                director.Tick(start, () => true);
                director.Tick(start + wave.spawnInterval, () => true);
                check(director.WaveNumber == i + 1 && director.SpawnedThisWave == wave.enemyCount, "Variable wave quota");
                director.Tick(start + wave.duration - 0.1f, () => throw new Exception("Spawn during rest"));
                start += wave.duration;
            }
            director.Tick(start, () => throw new Exception("Spawn after schedule"));
            check(director.State == WaveDirector.WaveState.Completed, "Schedule completion");

            plan.BackgroundWave.enabled = true;
            director.Begin(plan);
            int attempts = 0;
            director.Tick(0f, () => { attempts++; return false; });
            check(attempts == 2 && director.ConstantSpawned == 0 && director.SpawnedThisWave == 0, "Shared cap rejects both without consuming quotas");
            director.Tick(0.5f, () => throw new Exception("Early retry"));
            director.Tick(1f, () => true);
            check(director.SpawnedThisWave == 1 && director.ConstantSpawned == 0, "Independent recovery interval");
            director.Tick(2f, () => true);
            check(director.SpawnedThisWave == 2 && director.ConstantSpawned == 1, "Independent successful counts");
            director.Tick(4f, () => true);
            check(director.WaveNumber == 2 && director.ConstantSpawned == 2, "Constant wave across boundary");
            director.Tick(5f, () => true);
            director.Tick(6f, () => true);
            check(director.State == WaveDirector.WaveState.WaitingForNextWave && director.ConstantSpawned == 3, "Constant spawn during rest");
            director.Tick(plan.TotalDuration + 20f, () => true);
            check(director.State == WaveDirector.WaveState.Completed && director.ConstantSpawned == 4, "Long frame has no catch-up burst");
            director.Tick(plan.TotalDuration + 22f, () => true);
            check(director.ConstantSpawned == 5, "Constant wave continues after schedule");
            director.Stop();
            director.Tick(plan.TotalDuration + 40f, () => throw new Exception("Spawn after stop"));
            check(director.State == WaveDirector.WaveState.Stopped, "Match end stops both streams");
            director.Begin(plan);
            check(director.WaveNumber == 1 && director.SpawnedThisWave == 0 && director.ConstantSpawned == 0, "Restart resets both streams");
            director.Tick(0f, () => true);
            check(director.ConstantSpawned == 1 && director.SpawnedThisWave == 1, "Restart resets both clocks");
            director.Begin(plan);
            director.Tick(durations[0] + durations[1], () => true);
            check(director.WaveNumber == 3 && director.SpawnedThisWave == 1, "Expired wave quotas skipped");
            plan.BackgroundWave.spawnInterval = float.NaN;
            check(!plan.Validate(out _), "Invalid constant interval rejected");
            plan.BackgroundWave.spawnInterval = 2f;
            plan.GetWave(0).spawnInterval = 0f;
            check(!plan.Validate(out _), "Invalid scheduled interval rejected");
            plan.GetWave(0).spawnInterval = 1f;
            check(!plan.ValidateForMatch(float.PositiveInfinity, out _), "Invalid match duration rejected");
        }
        finally { UnityEngine.Object.DestroyImmediate(plan); }
        return $"Wave system: {checks} checks passed.";
    }
}

[CustomEditor(typeof(MatchConfig))]
public class MatchConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var settings = (MatchConfig)target;
        foreach (var profile in settings.Difficulties)
        {
            if (profile == null || profile.WavePlan == null)
                EditorGUILayout.HelpBox("Assign each difficulty and its wave plan.", MessageType.Warning);
            else if (!profile.WavePlan.ValidateForMatch(settings.Duration, out string error))
                EditorGUILayout.HelpBox(profile.DisplayName + ": " + error, MessageType.Warning);
        }
        if (GUILayout.Button("Check Wave System")) Debug.Log(WaveSystemChecks.Run(settings));
    }
}
