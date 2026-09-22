using UnityEngine;

[RequireComponent(typeof(Health))]
public class AutoAttackController : MonoBehaviour
{
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private Transform rifle;
    [SerializeField] private Vector3 rifleAimOffset = new Vector3(0f, 180f, 0f);
    [SerializeField] private Transform muzzle;
    [SerializeField] private ParticleSystem muzzleFlashPrefab;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform projectilesParent;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform aimingVisual;
    [SerializeField] private LineRenderer attackRangeRingRenderer;
    [SerializeField] private float visualAimYawOffset = 32f;
    [SerializeField, Min(1f)] private float aimRotationSpeed = 720f;
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
    private Quaternion smoothedRifleRotation;
    private Quaternion smoothedVisualRotation;
    private const int AttackRangeRingSegments = 96;

    private void Awake()
    {
        health = GetComponent<Health>();
        if (rifle != null)
            restingRifleRotation = rifle.localRotation;
        if (aimingVisual != null)
            restingVisualRotation = aimingVisual.localRotation;

        UpdateAttackRangeRing();
    }

    private void UpdateAttackRangeRing()
    {
        if (attackRangeRingRenderer == null)
            return;

        attackRangeRingRenderer.positionCount = AttackRangeRingSegments;
        float radius = Mathf.Max(0f, attackRange);
        for (int i = 0; i < AttackRangeRingSegments; i++)
        {
            float angle = i * (Mathf.PI * 2f / AttackRangeRingSegments);
            attackRangeRingRenderer.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f,
                Mathf.Sin(angle) * radius));
        }
    }

    private void OnValidate()
    {
        UpdateAttackRangeRing();
    }

    private void OnEnable()
    {
        // The idle animation can move the rifle while gameplay is disabled in menus.
        if (rifle != null)
            smoothedRifleRotation = rifle.rotation;
        if (aimingVisual != null)
            smoothedVisualRotation = aimingVisual.rotation;
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
            if (aimingVisual != null)
                SmoothAim(aimingVisual, ref smoothedVisualRotation,
                    ParentRotation(aimingVisual) * restingVisualRotation);
            if (rifle != null)
                SmoothAim(rifle, ref smoothedRifleRotation,
                    ParentRotation(rifle) * restingRifleRotation);
            return;
        }

        // Turn the animated visual, not the gameplay root, so the arms follow the aim.
        if (aimingVisual != null)
        {
            Vector3 visualDirection = CurrentTarget.transform.position - aimingVisual.position;
            visualDirection.y = 0f;
            if (visualDirection.sqrMagnitude > 0.0001f)
                SmoothAim(aimingVisual, ref smoothedVisualRotation,
                    Quaternion.LookRotation(visualDirection) * Quaternion.Euler(0f, visualAimYawOffset, 0f));
        }

        if (rifle != null)
        {
            Vector3 direction = CurrentTarget.Health.AimPosition - rifle.position;
            if (direction.sqrMagnitude > 0.0001f)
                SmoothAim(rifle, ref smoothedRifleRotation,
                    Quaternion.LookRotation(direction) * Quaternion.Euler(rifleAimOffset));
        }

        if (Time.time < nextFire || projectilePrefab == null || muzzle == null)
            return;

        nextFire = Time.time + Mathf.Max(0.02f, fireInterval);
        Projectile projectile = Instantiate(projectilePrefab, muzzle.position, muzzle.rotation, projectilesParent);
        projectile.Initialize(CurrentTarget.Health, projectileDamage);
        if (muzzleFlashPrefab != null)
            Instantiate(muzzleFlashPrefab, muzzle.position, muzzle.rotation, muzzle);
        if (animator != null && animator.isActiveAndEnabled)
        {
            animator.ResetTrigger("Fire");
            animator.SetTrigger("Fire");
        }
        if (audioManager != null)
            audioManager.PlayShot();
    }

    private void SmoothAim(Transform aim, ref Quaternion rotation, Quaternion desired)
    {
        // Keep world-space history so locomotion turns and animated hand motion
        // cannot introduce a new snap before this LateUpdate.
        rotation = Quaternion.RotateTowards(rotation, desired, aimRotationSpeed * Time.deltaTime);
        aim.rotation = rotation;
    }

    private static Quaternion ParentRotation(Transform child) =>
        child.parent != null ? child.parent.rotation : Quaternion.identity;

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
