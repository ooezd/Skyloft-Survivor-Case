using UnityEngine;

[CreateAssetMenu(menuName = "Skyloft/Difficulty Profile", fileName = "New Difficulty")]
public class DifficultyConfig : ScriptableObject
{
    [Header("Difficulty")]
    [SerializeField] private string displayName = "Normal";
    [TextArea, SerializeField] private string description;
    [Header("Enemy Population")]
    [Tooltip("Maximum living enemies across all waves, not the total enemies in one wave.")]
    [SerializeField, Min(1)] private int maximumActiveEnemies = 25;
    [Header("Wave Schedule")]
    [Tooltip("Ordered wave settings. The schedule must cover the entire match.")]
    [SerializeField] private WavePlanConfig wavePlan;

    public string DisplayName => displayName;
    public int MaximumActiveEnemies => Mathf.Max(1, maximumActiveEnemies);
    public WavePlanConfig WavePlan => wavePlan;
}
