using UnityEngine;

[RequireComponent(typeof(Health))]
public class DamageFlash : MonoBehaviour
{
    [SerializeField] private Renderer[] modelRenderers = new Renderer[0];
    [SerializeField] private Material flashMaterial;
    [SerializeField, Range(0.05f, 0.12f)] private float duration = 0.09f;

    private Health health;
    private Material[][] originalMaterials;
    private Material[][] flashMaterials;
    private float remaining;
    private bool flashing;

    private void Awake()
    {
        health = GetComponent<Health>();
        originalMaterials = new Material[modelRenderers.Length][];
        flashMaterials = new Material[modelRenderers.Length][];
        for (int i = 0; i < modelRenderers.Length; i++)
        {
            if (modelRenderers[i] == null)
                continue;
            originalMaterials[i] = modelRenderers[i].sharedMaterials;
            flashMaterials[i] = new Material[originalMaterials[i].Length];
            for (int slot = 0; slot < flashMaterials[i].Length; slot++)
                flashMaterials[i][slot] = flashMaterial;
        }
    }

    private void OnEnable() => health.Damaged += OnDamaged;

    private void OnDisable()
    {
        health.Damaged -= OnDamaged;
        Restore();
    }

    private void OnDamaged(Health damaged)
    {
        if (flashMaterial == null)
            return;
        remaining = duration;
        if (flashing)
            return;
        flashing = true;
        for (int i = 0; i < modelRenderers.Length; i++)
            if (modelRenderers[i] != null)
                modelRenderers[i].sharedMaterials = flashMaterials[i];
    }

    private void Update()
    {
        if (!flashing)
            return;
        remaining -= Time.deltaTime;
        if (remaining <= 0f)
            Restore();
    }

    private void Restore()
    {
        if (!flashing)
            return;
        for (int i = 0; i < modelRenderers.Length; i++)
            if (modelRenderers[i] != null)
                modelRenderers[i].sharedMaterials = originalMaterials[i];
        flashing = false;
        remaining = 0f;
    }
}
