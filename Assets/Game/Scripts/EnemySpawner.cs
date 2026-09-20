using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private EnemyController enemyPrefab;
    [SerializeField] private Transform player;
    [SerializeField] private Transform enemiesParent;
    [SerializeField, Min(0.1f)] private float spawnInterval = 1f;
    [SerializeField, Min(1f)] private float spawnRadius = 14f;
    [SerializeField, Min(0)] private int maximumActiveEnemies = 20;

    private readonly List<EnemyController> activeEnemies = new List<EnemyController>();
    private float timeUntilSpawn;
    private Health playerHealth;
    [SerializeField] private int killCount;

    public IReadOnlyList<EnemyController> ActiveEnemies => activeEnemies;
    public int KillCount => killCount;

    private void Awake()
    {
        killCount = 0;
        if (player != null)
            playerHealth = player.GetComponent<Health>();
    }

    private void HandleEnemyDeath(Health enemyHealth)
    {
        enemyHealth.Died -= HandleEnemyDeath;
        if (activeEnemies.Remove(enemyHealth.GetComponent<EnemyController>()))
            killCount++;
    }

    private void OnDestroy()
    {
        foreach (EnemyController enemy in activeEnemies)
            if (enemy != null && enemy.Health != null)
                enemy.Health.Died -= HandleEnemyDeath;
    }

    private void Update()
    {
        if (enemyPrefab == null || player == null || playerHealth == null || !playerHealth.IsAlive)
            return;

        timeUntilSpawn -= Time.deltaTime;
        if (timeUntilSpawn > 0f)
            return;

        timeUntilSpawn = Mathf.Max(0.1f, spawnInterval);
        activeEnemies.RemoveAll(enemy => enemy == null);
        if (activeEnemies.Count >= maximumActiveEnemies)
            return;

        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * Mathf.Max(1f, spawnRadius);
        EnemyController enemy = Instantiate(enemyPrefab, player.position + offset, Quaternion.identity, enemiesParent);
        enemy.SetTarget(player);
        activeEnemies.Add(enemy);
        enemy.Health.Died += HandleEnemyDeath;
    }
}
