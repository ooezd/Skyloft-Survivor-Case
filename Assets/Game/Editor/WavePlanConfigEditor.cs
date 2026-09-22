using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WavePlanConfig))]
public class WavePlanConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("description"));
        var waves = serializedObject.FindProperty("waves");
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Waves run in order. Living enemies carry over; unspawned quotas expire. Duration includes the rest after spawning.", MessageType.Info);
        int count = Mathf.Max(0, EditorGUILayout.IntField("Number of Waves", waves.arraySize));
        if (count != waves.arraySize) waves.arraySize = count;
        float start = 0f;
        for (int i = 0; i < waves.arraySize; i++)
        {
            var wave = waves.GetArrayElementAtIndex(i);
            var duration = wave.FindPropertyRelative("duration");
            var quota = wave.FindPropertyRelative("enemyCount");
            var interval = wave.FindPropertyRelative("spawnInterval");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Wave {i + 1} | {start:0.#} - {start + duration.floatValue:0.#} seconds", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(wave.FindPropertyRelative("name"), new GUIContent("Label"));
            EditorGUILayout.PropertyField(duration, new GUIContent("Duration (seconds)"));
            EditorGUILayout.PropertyField(quota, new GUIContent("Enemy Count (total quota)"));
            EditorGUILayout.PropertyField(interval, new GUIContent("Spawn Interval (seconds)"));
            float lastSpawn = (quota.intValue - 1) * interval.floatValue;
            EditorGUILayout.LabelField($"Without cap: last spawn at +{lastSpawn:0.#}s; rest {Mathf.Max(0, duration.floatValue - lastSpawn):0.#}s", EditorStyles.miniLabel);
            if (lastSpawn >= duration.floatValue)
                EditorGUILayout.HelpBox("The quota cannot finish within this duration, even with free capacity.", MessageType.Warning);
            EditorGUILayout.EndVertical();
            start += duration.floatValue;
        }
        serializedObject.ApplyModifiedProperties();
        EditorGUILayout.LabelField($"Total Schedule: {start:0.#} seconds", EditorStyles.boldLabel);
        if (!((WavePlanConfig)target).ValidateForMatch(180f, out string error))
            EditorGUILayout.HelpBox(error, MessageType.Warning);
    }
}
