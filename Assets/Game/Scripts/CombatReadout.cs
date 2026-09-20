using UnityEngine;
using UnityEngine.UI;

public class CombatReadout : MonoBehaviour
{
    [SerializeField] private Health playerHealth;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private Text label;

    private int displayedHealth = -1;
    private int displayedKills = -1;

    private void Update()
    {
        if (playerHealth == null || enemySpawner == null || label == null)
            return;
        if (displayedHealth == playerHealth.CurrentHealth && displayedKills == enemySpawner.KillCount)
            return;

        displayedHealth = playerHealth.CurrentHealth;
        displayedKills = enemySpawner.KillCount;
        label.text = $"Health: {displayedHealth}/{playerHealth.MaxHealth}   Kills: {displayedKills}";
        label.color = playerHealth.IsAlive ? Color.white : new Color(1f, 0.35f, 0.35f);
    }
}
