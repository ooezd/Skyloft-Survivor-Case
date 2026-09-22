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
    [SerializeField, Range(0.15f, 0.3f)] private float deathDuration = 0.24f;

    public Health Health { get; private set; }
    private Health targetHealth;
    private float nextDamageTime;
    private EnemySpawner spawner;

    private void Awake()
    {
        Health = GetComponent<Health>();
        if (target != null)
            targetHealth = target.GetComponent<Health>();
    }

    private void OnEnable() => Health.Died += HandleDeath;

    private void OnDisable() => Health.Died -= HandleDeath;

    private void HandleDeath(Health deadHealth)
    {
        // Health raises Died immediately, so the spawner removes/counts this enemy now.
        foreach (Animator animator in GetComponentsInChildren<Animator>())
            animator.enabled = false;
        foreach (Collider collider in GetComponentsInChildren<Collider>())
            collider.enabled = false;
        StartCoroutine(ShowDeath());
    }

    private System.Collections.IEnumerator ShowDeath()
    {
        Quaternion rotation = transform.rotation;
        Vector3 scale = transform.localScale;
        float elapsed = 0f;
        while (elapsed < deathDuration)
        {
            float t = Mathf.Clamp01(elapsed / deathDuration);
            transform.rotation = rotation * Quaternion.Euler(-65f * t, 0f, 15f * t);
            transform.localScale = scale * Mathf.Lerp(1f, 0.3f, t * t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }

    public void SetSpawner(EnemySpawner owner) => spawner = owner;

    private Vector3 Separation()
    {
        Vector3 force = Vector3.zero;
        if (spawner == null || separationWeight <= 0f)
            return force;

        foreach (EnemyController other in spawner.ActiveEnemies)
        {
            if (other == null || other == this || !other.isActiveAndEnabled || !other.Health.IsAlive)
                continue;
            Vector3 offset = transform.position - other.transform.position;
            offset.y = 0f;
            float distance = offset.magnitude;
            if (distance >= separationRadius)
                continue;
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
        if (!Health.IsAlive || target == null || targetHealth == null || !targetHealth.IsAlive)
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
