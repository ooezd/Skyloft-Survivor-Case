using UnityEngine;

public sealed class UpgradePickupSpawner : MonoBehaviour
{
    [SerializeField] private UpgradePickup pickupPrefab;
    [SerializeField] private PlayerController player;
    [SerializeField] private AutoAttackController playerAttack;
    [SerializeField] private Health playerHealth;
    [Header("Spawn Settings")]
    [SerializeField, Min(0f)] private float firstSpawnDelay = 2f;
    [SerializeField, Min(0.1f)] private float spawnInterval = 10f;
    [SerializeField, Min(0.1f)] private float pickupLifetime = 25f;
    [SerializeField, Min(0f)] private float minimumDistance = 4f;
    [SerializeField, Min(0f)] private float maximumDistance = 8f;
    [Tooltip("Cube center height above the player's ground level.")]
    [SerializeField] private float groundOffset = 0.25f;

    public AutoAttackController PlayerAttack => playerAttack;
    private bool running;
    private float nextSpawn;
    private UpgradePickup activePickup;

    public void BeginRun()
    {
        EndRun();
        running = true;
        nextSpawn = Time.time + Mathf.Max(0f, firstSpawnDelay);
    }

    public void EndRun()
    {
        running = false;
        if (activePickup != null)
        {
            activePickup.gameObject.SetActive(false);
            Destroy(activePickup.gameObject);
            activePickup = null;
        }
    }

    public bool CanCollect(Health collector) => running && collector != null &&
        collector == playerHealth && collector.isActiveAndEnabled && collector.IsAlive &&
        playerAttack != null && playerAttack.isActiveAndEnabled;

    private void Update()
    {
        if (!running || !CanCollect(playerHealth) || player == null || pickupPrefab == null ||
            activePickup != null || Time.time < nextSpawn)
            return;

        nextSpawn = Time.time + Mathf.Max(0.1f, spawnInterval);
        float min = Mathf.Max(0f, minimumDistance);
        float max = Mathf.Max(min, maximumDistance);
        for (int attempt = 0; attempt < 20; attempt++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Mathf.Sqrt(Random.Range(min * min, max * max));
            Vector3 position = player.transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            position = player.ClampToArena(position);
            Vector3 offset = position - player.transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude < min * min)
                continue;
            position.y = player.transform.position.y + groundOffset;
            activePickup = Instantiate(pickupPrefab, position, pickupPrefab.transform.rotation, transform);
            activePickup.Initialize(this, pickupLifetime);
            return;
        }
    }

    private void OnDisable() => EndRun();
}
