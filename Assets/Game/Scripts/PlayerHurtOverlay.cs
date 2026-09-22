using UnityEngine;
using UnityEngine.UI;

public class PlayerHurtOverlay : MonoBehaviour
{
    [SerializeField] private Health playerHealth;
    [SerializeField] private Image hurtScreen;
    [SerializeField, Range(0f, 1f)] private float damagePulseAlpha = 0.42f;
    [SerializeField, Min(0f)] private float fadeInDuration = 0.025f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.28f;
    [SerializeField, Range(0f, 1f)] private float criticalThreshold = 0.3f;
    [SerializeField, Range(0f, 1f)] private float criticalRestingAlpha = 0.12f;
    [SerializeField, Range(0f, 1f)] private float deathAlpha = 0.28f;

    private float restingAlpha;
    private float pulseStartAlpha;
    private float elapsed;
    private bool pulsing;

    private void OnEnable()
    {
        if (playerHealth == null)
            return;
        playerHealth.Changed += OnHealthChanged;
        playerHealth.Damaged += OnDamaged;
        OnHealthChanged(playerHealth);
    }

    // Health.Awake has completed on every object before Start.
    private void Start()
    {
        if (playerHealth != null)
            OnHealthChanged(playerHealth);
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.Changed -= OnHealthChanged;
            playerHealth.Damaged -= OnDamaged;
        }
        pulsing = false;
        SetAlpha(0f);
    }

    private void OnHealthChanged(Health health)
    {
        restingAlpha = !health.IsAlive ? deathAlpha :
            ((float)health.CurrentHealth / health.MaxHealth < criticalThreshold ? criticalRestingAlpha : 0f);
        if (!health.IsAlive)
            pulsing = false;
        if (!pulsing)
            SetAlpha(restingAlpha);
    }

    private void OnDamaged(Health health)
    {
        if (!health.IsAlive || hurtScreen == null)
            return;
        pulseStartAlpha = hurtScreen.color.a;
        elapsed = 0f;
        pulsing = true;
        if (fadeInDuration <= 0f)
            SetAlpha(damagePulseAlpha);
    }

    private void Update()
    {
        if (!pulsing)
            return;
        elapsed += Time.unscaledDeltaTime;
        if (elapsed < fadeInDuration)
            SetAlpha(Mathf.Lerp(pulseStartAlpha, damagePulseAlpha, elapsed / fadeInDuration));
        else
        {
            float t = fadeOutDuration > 0f ? (elapsed - fadeInDuration) / fadeOutDuration : 1f;
            SetAlpha(Mathf.Lerp(damagePulseAlpha, restingAlpha, t));
            if (t >= 1f)
                pulsing = false;
        }
    }

    private void SetAlpha(float alpha)
    {
        if (hurtScreen == null)
            return;
        Color color = hurtScreen.color;
        color.a = alpha;
        hurtScreen.color = color;
    }
}
