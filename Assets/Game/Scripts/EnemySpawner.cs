using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private EnemyController enemyPrefab;
    [SerializeField] private Transform player;
    [SerializeField] private Transform enemiesParent;
    [SerializeField, Min(1f)] private float spawnRadius = 14f;
    [SerializeField, Min(0)] private int maximumActiveEnemies = 20;

    private readonly List<EnemyController> activeEnemies = new List<EnemyController>();
    private readonly Stack<EnemyController> availableEnemies = new Stack<EnemyController>();
    private Health playerHealth;
    [SerializeField] private int killCount;

    public IReadOnlyList<EnemyController> ActiveEnemies => activeEnemies;
    public int KillCount => killCount;

#if UNITY_EDITOR || SKYLOFT_BENCHMARK
    private System.Random benchmarkRandom;
    public int BenchmarkSpawnCount { get; private set; }
    public void SetBenchmarkSeed(int seed)
    {
        benchmarkRandom = new System.Random(seed);
        BenchmarkSpawnCount = 0;
    }
#endif

    public void ApplyDifficulty(DifficultyConfig difficulty)
    {
        maximumActiveEnemies = Mathf.Max(1, difficulty.MaximumActiveEnemies);
    }

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

    public bool TrySpawnEnemy()
    {
        if (!isActiveAndEnabled || enemyPrefab == null || player == null || playerHealth == null || !playerHealth.IsAlive)
            return false;

        activeEnemies.RemoveAll(enemy => enemy == null);
        if (activeEnemies.Count >= maximumActiveEnemies)
            return false;

        float angle = Random.Range(0f, Mathf.PI * 2f);
#if UNITY_EDITOR || SKYLOFT_BENCHMARK
        if (benchmarkRandom != null)
            angle = (float)benchmarkRandom.NextDouble() * Mathf.PI * 2f;
#endif
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * Mathf.Max(1f, spawnRadius);
        EnemyController enemy = null;
        while (availableEnemies.Count > 0 && enemy == null)
            enemy = availableEnemies.Pop();
        if (enemy == null)
        {
            enemy = Instantiate(enemyPrefab, player.position + offset, Quaternion.identity, enemiesParent);
            enemy.SetTarget(player);
            enemy.SetSpawner(this);
        }
        else
        {
            enemy.transform.SetPositionAndRotation(player.position + offset, Quaternion.identity);
            enemy.PrepareForSpawn(player, this);
            enemy.gameObject.SetActive(true);
            enemy.ResetPresentationForSpawn();
        }
        activeEnemies.Add(enemy);
        enemy.Health.Died += HandleEnemyDeath;
#if UNITY_EDITOR || SKYLOFT_BENCHMARK
        if (benchmarkRandom != null)
            BenchmarkSpawnCount++;
#endif
        return true;
    }

    public void ReturnEnemy(EnemyController enemy)
    {
        if (enemy == null) return;
        enemy.gameObject.SetActive(false);
        availableEnemies.Push(enemy);
    }
}
