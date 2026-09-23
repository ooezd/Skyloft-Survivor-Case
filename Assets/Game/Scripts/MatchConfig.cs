using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Skyloft/Match Settings", fileName = "Match Settings")]
public class MatchConfig : ScriptableObject
{
    [SerializeField, Min(0.1f)] private float duration = 180f;
    [SerializeField] private DifficultyConfig[] difficulties = new DifficultyConfig[0];
    public float Duration => duration;
    public IReadOnlyList<DifficultyConfig> Difficulties => difficulties;
}
