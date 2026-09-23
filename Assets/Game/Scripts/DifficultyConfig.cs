using UnityEngine;

[CreateAssetMenu(menuName = "Skyloft/Difficulty Profile", fileName = "New Difficulty")]
public class DifficultyConfig : ScriptableObject
{
    [Header("Difficulty")]
    [SerializeField] private string displayName;
    [TextArea, SerializeField] private string description;
    [Header("Enemy Population")]
    [Tooltip("Maximum living enemies across all waves, not the total enemies in one wave.")]
    [SerializeField, Min(1)] private int maximumActiveEnemies = 25;
    [Header("Wave Schedule")]
    [Tooltip("Ordered waves and an independent constant wave. Without a constant wave, the schedule must cover the match.")]
    [SerializeField] private WavePlanConfig wavePlan;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public int MaximumActiveEnemies => Mathf.Max(1, maximumActiveEnemies);
    public WavePlanConfig WavePlan => wavePlan;
}
