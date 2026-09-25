using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float speed = 18f;
    [SerializeField, Min(0f)] private float impactRadius = 0.2f;
    [SerializeField, Min(0.1f)] private float lifetime = 5f;
    [SerializeField] private ParticleSystem impactPrefab;

    private Health target;
    private int damage;
    private int targetSpawnVersion;
    private AutoAttackController owner;
    private TrailRenderer trail;
    private float elapsed;
    private bool released;

    private void Awake() => trail = GetComponentInChildren<TrailRenderer>(true);

    public void Initialize(Health selectedTarget, int impactDamage, AutoAttackController source)
    {
        target = selectedTarget;
        damage = impactDamage;
        targetSpawnVersion = selectedTarget != null ? selectedTarget.SpawnVersion : 0;
        owner = source;
        elapsed = 0f;
        released = false;
        if (trail != null) trail.Clear();
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= Mathf.Max(0.1f, lifetime) || target == null ||
            !target.isActiveAndEnabled || !target.IsAlive || target.SpawnVersion != targetSpawnVersion)
        {
            Release();
            return;
        }

        Vector3 destination = target.AimPosition;
        Vector3 direction = destination - transform.position;
        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(direction);

        transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
        if ((destination - transform.position).sqrMagnitude <= impactRadius * impactRadius)
        {
            int healthBefore = target.CurrentHealth;
            target.TakeDamage(damage);
            if (impactPrefab != null && target.CurrentHealth < healthBefore)
            {
                if (owner != null) owner.PlayImpact(impactPrefab, destination);
                else Instantiate(impactPrefab, destination, Quaternion.identity);
            }
            Release();
        }
    }

    private void Release()
    {
        if (released) return;
        released = true;
        target = null;
        if (owner != null) owner.ReturnProjectile(this);
        else Destroy(gameObject);
    }
}
