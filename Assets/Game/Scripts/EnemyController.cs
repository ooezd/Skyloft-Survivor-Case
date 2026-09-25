using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemyController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField, Min(0f)] private float movementSpeed = 1.8f;
    [SerializeField, Min(0f)] private float contactRange = 1.2f;
    [SerializeField, Min(1)] private int contactDamage = 10;
    [SerializeField, Min(0.02f)] private float damageInterval = 1f;
    [SerializeField, Min(0.01f)] private float separationRadius = 1.5f;
    [SerializeField, Min(0f)] private float separationWeight = 2.2f;
    [SerializeField] private DeathAnimation deathAnimation;

    [SerializeField, Range(0.15f, 0.6f)] private float spawnDuration = 0.35f;
    private Coroutine spawnSequence;
    private bool isSpawning;
    private Vector3 originalScale;

    public Health Health { get; private set; }
    private Health targetHealth;
    private float nextDamageTime;
    private EnemySpawner spawner;

    private void Awake()
    {
        Health = GetComponent<Health>();
        originalScale = transform.localScale;
        if (target != null)
            targetHealth = target.GetComponent<Health>();
    }

    private void OnEnable()
    {
        Health.Died += HandleDeath;
        Health.DamageReceived += ShowDamageNumber;
        spawnSequence = StartCoroutine(ShowSpawn());
    }

    private System.Collections.IEnumerator ShowSpawn()
    {
        isSpawning = true;
        Vector3 scale = transform.localScale;
        float elapsed = 0f;
        while (elapsed < spawnDuration)
        {
            float t = Mathf.Clamp01(elapsed / spawnDuration);
            // Ease out with a small overshoot, then settle at the prefab scale.
            float u = t - 1f;
            float eased = 1f + 2.70158f * u * u * u + 1.70158f * u * u;
            transform.localScale = scale * Mathf.LerpUnclamped(0.15f, 1f, eased);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = scale;
        isSpawning = false;
        spawnSequence = null;
    }

    private void OnDisable()
    {
        Health.Died -= HandleDeath;
        Health.DamageReceived -= ShowDamageNumber;
        if (spawnSequence != null)
        {
            StopCoroutine(spawnSequence);
            spawnSequence = null;
        }
        isSpawning = false;
    }

    private void ShowDamageNumber(Health damagedHealth, int damage)
    {
        DamageNumberCanvas.Show(damagedHealth.AimPosition, damage);
    }

    private void HandleDeath(Health deadHealth)
    {
        if (spawnSequence != null)
        {
            StopCoroutine(spawnSequence);
            spawnSequence = null;
        }
        isSpawning = false;
        // Health raises Died immediately, so the spawner removes/counts this enemy now.
        transform.localScale = originalScale;
        StartCoroutine(ShowDeath());
    }

    private System.Collections.IEnumerator ShowDeath()
    {
        if (deathAnimation != null)
            yield return deathAnimation.Play();
        if (spawner != null) spawner.ReturnEnemy(this);
        else Destroy(gameObject);
    }

    public void SetSpawner(EnemySpawner owner) => spawner = owner;

    public void PrepareForSpawn(Transform player, EnemySpawner owner)
    {
        SetTarget(player);
        spawner = owner;
        nextDamageTime = 0f;
        transform.localScale = originalScale;
        Health.ResetForSpawn();
    }

    public void ResetPresentationForSpawn()
    {
        if (deathAnimation != null)
            deathAnimation.ResetForSpawn();
    }

    private Vector3 Separation()
    {
        Vector3 force = Vector3.zero;
        if (spawner == null || separationWeight <= 0f)
            return force;

        // The spawner exposes IReadOnlyList; indexed access avoids an interface enumerator
        // for every enemy on every frame. Most enemies are outside this small radius, so
        // reject them before health checks and the square root.
        var enemies = spawner.ActiveEnemies;
        Vector3 position = transform.position;
        float radiusSquared = separationRadius * separationRadius;
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyController other = enemies[i];
            if (other == null || other == this)
                continue;
            Vector3 offset = position - other.transform.position;
            offset.y = 0f;
            float distanceSquared = offset.sqrMagnitude;
            if (distanceSquared >= radiusSquared || !other.isActiveAndEnabled || !other.Health.IsAlive)
                continue;
            float distance = Mathf.Sqrt(distanceSquared);
            // Opposite, deterministic directions also separate exact coincident spawns.
            Vector3 away = distance > 0.0001f ? offset / distance :
                (GetEntityId() < other.GetEntityId() ? Vector3.right : Vector3.left);
            force += away * (1f - distance / separationRadius);
        }
        return force * separationWeight;
    }

    public void SetTarget(Transform player)
    {
        target = player;
        targetHealth = player != null ? player.GetComponent<Health>() : null;
    }

    private void Update()
    {
        if (isSpawning || !Health.IsAlive || target == null || targetHealth == null || !targetHealth.IsAlive)
            return;

        Vector3 destination = new Vector3(target.position.x, transform.position.y, target.position.z);
        Vector3 direction = destination - transform.position;
        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(direction);

        float distance = direction.magnitude;
        // A short arrival band and unnormalized forces let a packed crowd settle
        // instead of taking full-speed steps back and forth at equilibrium.
        Vector3 seek = direction.normalized * Mathf.Clamp01((distance - contactRange) / 0.3f);
        Vector3 movement = Vector3.ClampMagnitude(seek + Separation(), 1f);
        if (movement.sqrMagnitude > 0.000001f)
        {
            Vector3 position = transform.position + movement * (movementSpeed * Time.deltaTime);
            Vector3 fromTarget = position - destination;
            if (fromTarget.sqrMagnitude < contactRange * contactRange && fromTarget.sqrMagnitude > 0.0001f)
                position = destination + fromTarget.normalized * contactRange;
            transform.position = position;
        }
        if ((destination - transform.position).sqrMagnitude <= (contactRange + 0.001f) * (contactRange + 0.001f) && Time.time >= nextDamageTime)
        {
            nextDamageTime = Time.time + Mathf.Max(0.02f, damageInterval);
            targetHealth.TakeDamage(contactDamage);
        }
    }
}
