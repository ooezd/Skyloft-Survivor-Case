using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float speed = 18f;
    [SerializeField, Min(0f)] private float impactRadius = 0.2f;
    [SerializeField, Min(0.1f)] private float lifetime = 5f;
    [SerializeField] private ParticleSystem impactPrefab;

    private Health target;
    private int damage;

    public void Initialize(Health selectedTarget, int impactDamage)
    {
        target = selectedTarget;
        damage = impactDamage;
        Destroy(gameObject, Mathf.Max(0.1f, lifetime));
    }

    private void Update()
    {
        if (target == null || !target.isActiveAndEnabled || !target.IsAlive)
        {
            Destroy(gameObject);
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
                Instantiate(impactPrefab, destination, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}
