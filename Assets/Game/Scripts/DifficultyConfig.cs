using UnityEngine;

[CreateAssetMenu(menuName = "Skyloft/Difficulty")]
public class DifficultyConfig : ScriptableObject
{
    [SerializeField] private string displayName = "Normal";
    [SerializeField, Min(0.1f)] private float spawnInterval = 0.8f;
    [SerializeField, Min(1)] private int maximumActiveEnemies = 25;

    public string DisplayName => displayName;
    public float SpawnInterval => spawnInterval;
    public int MaximumActiveEnemies => maximumActiveEnemies;
}
