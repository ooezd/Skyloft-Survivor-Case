using UnityEngine;
using UnityEngine.UI;

public class CombatReadout : MonoBehaviour
{
    [SerializeField] private Health playerHealth;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private Text label;
    [SerializeField] private GameManager gameManager;

    private int displayedHealth = -1;
    private int displayedKills = -1;
    private int displayedSeconds = -1;

    private void Update()
    {
        if (playerHealth == null || enemySpawner == null || label == null || gameManager == null)
            return;
        int seconds = Mathf.CeilToInt(gameManager.RemainingTime);
        if (displayedHealth == playerHealth.CurrentHealth && displayedKills == enemySpawner.KillCount &&
            displayedSeconds == seconds)
            return;

        displayedHealth = playerHealth.CurrentHealth;
        displayedKills = enemySpawner.KillCount;
        displayedSeconds = seconds;
        label.text = $"<size=18><color=#A9C4CF>HEALTH</color></size>  <b>{displayedHealth}/{playerHealth.MaxHealth}</b>\n" +
            $"<size=18><color=#A9C4CF>KILLS</color></size>  <b>{displayedKills}</b>     " +
            $"<size=18><color=#A9C4CF>TIME</color></size>  <b>{seconds / 60:00}:{seconds % 60:00}</b>";
        label.color = playerHealth.IsAlive ? Color.white : new Color(1f, 0.35f, 0.35f);
    }
}
