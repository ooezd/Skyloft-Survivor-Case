using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemyController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField, Min(0f)] private float movementSpeed = 1.8f;
    [SerializeField, Min(0f)] private float contactRange = 1.2f;
    [SerializeField, Min(1)] private int contactDamage = 10;
    [SerializeField, Min(0.02f)] private float damageInterval = 1f;

    public Health Health { get; private set; }
    private Health targetHealth;
    private float nextDamageTime;

    private void Awake()
    {
        Health = GetComponent<Health>();
        if (target != null)
            targetHealth = target.GetComponent<Health>();
    }

    private void OnEnable() => Health.Died += HandleDeath;

    private void OnDisable() => Health.Died -= HandleDeath;

    private void HandleDeath(Health deadHealth) => Destroy(gameObject);

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
        float step = Mathf.Min(movementSpeed * Time.deltaTime, Mathf.Max(0f, distance - contactRange));
        transform.position = Vector3.MoveTowards(transform.position, destination, step);
        if (distance - step <= contactRange + 0.001f && Time.time >= nextDamageTime)
        {
            nextDamageTime = Time.time + Mathf.Max(0.02f, damageInterval);
            targetHealth.TakeDamage(contactDamage);
        }
    }
}
