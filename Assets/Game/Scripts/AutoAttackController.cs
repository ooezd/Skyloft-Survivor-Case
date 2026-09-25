using System.Collections.Generic;
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
    public float DamageMultiplier { get; private set; } = 1f;
    public float AttackFrequencyMultiplier { get; private set; } = 1f;
    public int CurrentDamage => Mathf.Max(1, Mathf.RoundToInt(projectileDamage * DamageMultiplier));
    public float CurrentFireInterval => Mathf.Max(0.02f, fireInterval / AttackFrequencyMultiplier);

    public void ApplyRunUpgrade(bool damageUpgrade)
    {
        if (damageUpgrade)
            DamageMultiplier *= 1.1f;
        else
        {
            AttackFrequencyMultiplier *= 1.1f;
            nextFire = Time.time + Mathf.Max(0f, nextFire - Time.time) / 1.1f;
        }
    }

    public void ResetRunUpgrades()
    {
        DamageMultiplier = AttackFrequencyMultiplier = 1f;
        nextFire = 0f;
    }

    private Health health;
    private float nextTargetRefresh;
    private float nextFire;
    private Quaternion restingRifleRotation;
    private Quaternion restingVisualRotation;
    private Quaternion smoothedRifleRotation;
    private Quaternion smoothedVisualRotation;
    private readonly Stack<Projectile> availableProjectiles = new Stack<Projectile>(16);
    private readonly Stack<ParticleSystem> availableMuzzleEffects = new Stack<ParticleSystem>(8);
    private readonly Stack<ParticleSystem> availableImpactEffects = new Stack<ParticleSystem>(8);
    private readonly List<ActiveEffect> activeEffects = new List<ActiveEffect>(16);
    private const int AttackRangeRingSegments = 96;

    private struct ActiveEffect
    {
        public ParticleSystem Effect;
        public bool IsImpact;
    }

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
        RecycleFinishedEffects();
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

        nextFire = Time.time + CurrentFireInterval;
        Projectile projectile = GetProjectile();
        projectile.Initialize(CurrentTarget.Health, CurrentDamage, this);
        if (muzzleFlashPrefab != null)
            PlayEffect(muzzleFlashPrefab, muzzle.position, muzzle.rotation, muzzle,
                availableMuzzleEffects, false);
        if (animator != null && animator.isActiveAndEnabled)
        {
            animator.ResetTrigger("Fire");
            animator.SetTrigger("Fire");
        }
        if (audioManager != null)
            audioManager.PlayShot();
    }

    private Projectile GetProjectile()
    {
        Projectile projectile = null;
        while (availableProjectiles.Count > 0 && projectile == null)
            projectile = availableProjectiles.Pop();
        if (projectile == null)
            return Instantiate(projectilePrefab, muzzle.position, muzzle.rotation, projectilesParent);

        projectile.transform.SetPositionAndRotation(muzzle.position, muzzle.rotation);
        projectile.gameObject.SetActive(true);
        return projectile;
    }

    public void ReturnProjectile(Projectile projectile)
    {
        if (projectile == null) return;
        projectile.gameObject.SetActive(false);
        availableProjectiles.Push(projectile);
    }

    public void PlayImpact(ParticleSystem prefab, Vector3 position)
    {
        if (prefab != null)
            PlayEffect(prefab, position, Quaternion.identity, projectilesParent,
                availableImpactEffects, true);
    }

    private void PlayEffect(ParticleSystem prefab, Vector3 position, Quaternion rotation,
        Transform parent, Stack<ParticleSystem> available, bool isImpact)
    {
        ParticleSystem effect = null;
        while (available.Count > 0 && effect == null)
            effect = available.Pop();
        if (effect == null)
        {
            effect = Instantiate(prefab, position, rotation, parent);
            var main = effect.main;
            main.stopAction = ParticleSystemStopAction.None;
        }
        else
        {
            effect.transform.SetPositionAndRotation(position, rotation);
            effect.gameObject.SetActive(true);
        }
        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        effect.Play(true);
        activeEffects.Add(new ActiveEffect { Effect = effect, IsImpact = isImpact });
    }

    private void RecycleFinishedEffects()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            ActiveEffect active = activeEffects[i];
            if (active.Effect != null && active.Effect.IsAlive(true)) continue;
            activeEffects.RemoveAt(i);
            if (active.Effect == null) continue;
            active.Effect.gameObject.SetActive(false);
            (active.IsImpact ? availableImpactEffects : availableMuzzleEffects).Push(active.Effect);
        }
    }

    private void OnDisable()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            ActiveEffect active = activeEffects[i];
            if (active.Effect == null) continue;
            active.Effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            active.Effect.gameObject.SetActive(false);
            (active.IsImpact ? availableImpactEffects : availableMuzzleEffects).Push(active.Effect);
        }
        activeEffects.Clear();
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
