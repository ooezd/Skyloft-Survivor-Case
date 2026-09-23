using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public sealed class DamageNumber : MonoBehaviour
{
    [Header("Prefab References")]
    [SerializeField] private Text label;
    [SerializeField] private CanvasGroup opacity;

    [Header("Animation")]
    [SerializeField, Min(0.01f)] private float lifetime = 0.8f;
    [Tooltip("Rise in canvas units, independent of the enemy's world scale.")]
    [SerializeField, Min(0f)] private float riseDistance = 60f;
    [SerializeField, Range(0f, 0.99f)] private float fadeStart = 0.3f;
    [SerializeField, Min(1f)] private float initialScale = 1.2f;
    [SerializeField, Min(0.01f)] private float popDuration = 0.16f;

    private RectTransform rect;
    private RectTransform container;
    private Canvas canvas;
    private Camera worldCamera;
    private Vector3 worldPosition;
    private Vector3 originalScale;
    private float originalAlpha;
    private float elapsed;
    private bool initialized;

    public void Initialize(int damage, Vector3 position, Camera camera, Canvas owner)
    {
        if (label == null || opacity == null || camera == null || owner == null)
        {
            Debug.LogError("Assign the damage number Text and CanvasGroup references on the prefab, and a camera on the canvas.", this);
            Destroy(gameObject);
            return;
        }

        rect = (RectTransform)transform;
        container = (RectTransform)rect.parent;
        canvas = owner;
        worldCamera = camera;
        // A fixed world position lets lethal hits outlive the enemy.
        worldPosition = position;
        originalScale = rect.localScale;
        originalAlpha = opacity.alpha;
        elapsed = 0f;
        label.text = damage.ToString(CultureInfo.InvariantCulture);
        initialized = true;
        UpdateVisual(0f);
    }

    private void LateUpdate()
    {
        if (!initialized)
            return;

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, lifetime));
        if (t >= 1f || worldCamera == null || canvas == null)
        {
            Destroy(gameObject);
            return;
        }
        UpdateVisual(t);
    }

    private void UpdateVisual(float t)
    {
        Vector3 screenPosition = worldCamera.WorldToScreenPoint(worldPosition);
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (screenPosition.z <= 0f || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                container, screenPosition, uiCamera, out Vector2 point))
        {
            opacity.alpha = 0f;
            return;
        }

        // Reproject each frame so camera movement does not detach the text from the hit location.
        rect.localPosition = new Vector3(point.x, point.y + riseDistance * t, 0f);
        rect.localScale = originalScale * Mathf.Lerp(initialScale, 1f,
            Mathf.Clamp01(elapsed / Mathf.Max(0.01f, popDuration)));
        opacity.alpha = originalAlpha * (1f - Mathf.InverseLerp(fadeStart, 1f, t));
    }
}
