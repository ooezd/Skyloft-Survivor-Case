using UnityEngine;

[RequireComponent(typeof(Health))]
public class AutoAttackController : MonoBehaviour
{
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private Transform rifle;
    [SerializeField] private Vector3 rifleAimOffset = new Vector3(0f, 180f, 0f);
    [SerializeField] private Transform muzzle;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform projectilesParent;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform aimingVisual;
    [SerializeField] private float visualAimYawOffset = 32f;
    [SerializeField, Min(0f)] private float attackRange = 8f;
    [SerializeField, Min(0.02f)] private float targetRefreshInterval = 0.2f;
    [SerializeField, Min(0.02f)] private float fireInterval = 0.5f;
    [SerializeField, Min(1)] private int projectileDamage = 25;

    public EnemyController CurrentTarget { get; private set; }

    private Health health;
    private float nextTargetRefresh;
    private float nextFire;
    private Quaternion restingRifleRotation;
    private Quaternion restingVisualRotation;

    private void Awake()
    {
        health = GetComponent<Health>();
        if (rifle != null)
            restingRifleRotation = rifle.localRotation;
        if (aimingVisual != null)
            restingVisualRotation = aimingVisual.localRotation;
    }

    private void LateUpdate()
    {
        if (!health.IsAlive)
        {
            CurrentTarget = null;
            return;
        }

        if (Time.time >= nextTargetRefresh)
        {
            nextTargetRefresh = Time.time + Mathf.Max(0.02f, targetRefreshInterval);
            AcquireTarget();
        }

        if (!IsValidTarget(CurrentTarget))
        {
            CurrentTarget = null;
            if (rifle != null)
                rifle.localRotation = restingRifleRotation;
            if (aimingVisual != null)
                aimingVisual.localRotation = restingVisualRotation;
            return;
        }

        // Turn the animated visual, not the gameplay root, so the arms follow the aim.
        if (aimingVisual != null)
        {
            Vector3 visualDirection = CurrentTarget.transform.position - aimingVisual.position;
            visualDirection.y = 0f;
            if (visualDirection.sqrMagnitude > 0.0001f)
                aimingVisual.rotation = Quaternion.LookRotation(visualDirection) * Quaternion.Euler(0f, visualAimYawOffset, 0f);
        }

        if (rifle != null)
        {
            Vector3 direction = CurrentTarget.Health.AimPosition - rifle.position;
            if (direction.sqrMagnitude > 0.0001f)
                rifle.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(rifleAimOffset);
        }

        if (Time.time < nextFire || projectilePrefab == null || muzzle == null)
            return;

        nextFire = Time.time + Mathf.Max(0.02f, fireInterval);
        Projectile projectile = Instantiate(projectilePrefab, muzzle.position, muzzle.rotation, projectilesParent);
        projectile.Initialize(CurrentTarget.Health, projectileDamage);
        if (animator != null && animator.isActiveAndEnabled)
        {
            animator.ResetTrigger("Fire");
            animator.SetTrigger("Fire");
        }
    }

    private void AcquireTarget()
    {
        CurrentTarget = null;
        if (enemySpawner == null)
            return;

        float nearestDistance = attackRange * attackRange;
        foreach (EnemyController enemy in enemySpawner.ActiveEnemies)
        {
            if (!IsValidTarget(enemy))
                continue;

            float distance = (enemy.transform.position - transform.position).sqrMagnitude;
            if (distance <= nearestDistance)
            {
                nearestDistance = distance;
                CurrentTarget = enemy;
            }
        }
    }

    private bool IsValidTarget(EnemyController enemy)
    {
        return enemy != null && enemy.isActiveAndEnabled && enemy.Health != null &&
            enemy.Health.IsAlive &&
            (enemy.transform.position - transform.position).sqrMagnitude <= attackRange * attackRange;
    }
}
