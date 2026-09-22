using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Skyloft/Wave Plan", fileName = "New Wave Plan")]
public class WavePlanConfig : ScriptableObject
{
    [Serializable]
    public class Wave
    {
        public string name = "Wave";
        [Tooltip("Seconds until the next wave, including the rest after spawning finishes.")]
        [Min(0.1f)] public float duration = 30f;
        [Tooltip("Spawn quota. Unspawned enemies expire when the wave ends.")]
        [Min(1)] public int enemyCount = 10;
        [Tooltip("Seconds between attempts. First attempt occurs at wave start.")]
        [Min(0.1f)] public float spawnInterval = 1f;
    }

    [TextArea, SerializeField] private string description;
    [Header("Waves - Played From Top To Bottom")]
    [SerializeField] private Wave[] waves = new Wave[0];
    public int WaveCount => waves == null ? 0 : waves.Length;
    public Wave GetWave(int index) => waves[index];
    public float TotalDuration
    {
        get
        {
            float total = 0f;
            if (waves != null)
                foreach (Wave wave in waves)
                    if (wave != null) total += wave.duration;
            return total;
        }
    }

    public bool ValidateForMatch(float matchDuration, out string error)
    {
        error = null;
        if (WaveCount == 0) error = "Add at least one wave.";
        else
        {
            for (int i = 0; i < waves.Length; i++)
            {
                Wave wave = waves[i];
                if (wave == null || !IsValidTime(wave.duration) || !IsValidTime(wave.spawnInterval) || wave.enemyCount < 1)
                {
                    error = $"Wave {i + 1} needs a duration/interval of at least 0.1 seconds and at least one enemy.";
                    break;
                }
            }
            if (error == null && TotalDuration + 0.001f < matchDuration)
                error = $"Wave plan covers {TotalDuration:0.##} seconds; match requires {matchDuration:0.##}.";
        }
        return error == null;
    }

    private static bool IsValidTime(float value) => value >= 0.1f && !float.IsInfinity(value) && !float.IsNaN(value);
}
