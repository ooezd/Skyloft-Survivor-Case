using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    [SerializeField] private Transform visual;
    [SerializeField, Min(0.01f)] private float hitPulseDuration = 0.15f;
    [SerializeField, Min(1f)] private float hitPulseScale = 1.18f;
    [SerializeField] private float aimHeight = 1.1f;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0;
    public int SpawnVersion { get; private set; } = 1;
    public Vector3 AimPosition => transform.position + Vector3.up * aimHeight;
    public event Action<Health> Died;
    public event Action<Health> Damaged;
    public event Action<Health, int> DamageReceived;
    public event Action<Health> Changed;

    private Vector3 originalScale;
    private float pulseRemaining;

#if UNITY_EDITOR || SKYLOFT_BENCHMARK
    public bool BenchmarkPreventDeath { get; set; }
#endif

    private void Awake()
    {
        currentHealth = Mathf.Max(1, maxHealth);
        if (visual != null)
            originalScale = visual.localScale;
    }

    public void ResetForSpawn()
    {
        ResetPulse();
        currentHealth = Mathf.Max(1, maxHealth);
        SpawnVersion++;
    }

    public void TakeDamage(int damage)
    {
        if (!isActiveAndEnabled || !IsAlive || damage <= 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
#if UNITY_EDITOR || SKYLOFT_BENCHMARK
        // Keep normal hit feedback and contact damage work, but finish the full workload.
        if (BenchmarkPreventDeath)
            currentHealth = Mathf.Max(1, currentHealth);
#endif
        pulseRemaining = hitPulseDuration;
        Changed?.Invoke(this);
        DamageReceived?.Invoke(this, damage);
        Damaged?.Invoke(this);
        if (!IsAlive)
        {
            ResetPulse();
            Died?.Invoke(this);
        }
    }

    // No healing pickup/mechanic is added; future recovery uses the same UI event.
    public void Heal(int amount)
    {
        if (!isActiveAndEnabled || !IsAlive || amount <= 0 || currentHealth >= maxHealth)
            return;

        currentHealth += Mathf.Min(amount, maxHealth - currentHealth);
        Changed?.Invoke(this);
    }

    private void Update()
    {
        if (visual == null || pulseRemaining <= 0f)
            return;

        pulseRemaining = Mathf.Max(0f, pulseRemaining - Time.deltaTime);
        visual.localScale = originalScale * Mathf.Lerp(1f, hitPulseScale,
            pulseRemaining / Mathf.Max(0.01f, hitPulseDuration));
    }

    private void OnDisable() => ResetPulse();

    private void ResetPulse()
    {
        pulseRemaining = 0f;
        if (visual != null)
            visual.localScale = originalScale;
    }
}
