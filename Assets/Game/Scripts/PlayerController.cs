using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private VirtualJoystick joystick;
    [SerializeField, Min(0f)] private float movementSpeed = 5f;
    [Tooltip("Center of the world-axis-aligned XZ movement bounds. Rotation and scale are ignored. Unassigned uses world origin.")]
    [SerializeField] private Transform arenaCenter;
    [SerializeField] private Vector2 arenaHalfExtents = new Vector2(180f, 180f);
    [SerializeField] private Transform visual;
    [SerializeField] private Animator animator;

    [Header("Death Presentation")]
    [SerializeField, Range(0.2f, 1.2f)] private float deathDuration = 0.55f;

    private Health health;

    private void Awake() => health = GetComponent<Health>();

    private void OnDisable()
    {
        if (animator != null)
            animator.SetFloat("Speed", 0f);
    }

    // GameManager runs this after combat stops and before revealing the result panel.
    public System.Collections.IEnumerator ShowDeath()
    {
        foreach (Animator childAnimator in GetComponentsInChildren<Animator>())
            childAnimator.enabled = false;
        foreach (Collider childCollider in GetComponentsInChildren<Collider>())
            childCollider.enabled = false;
        if (visual == null)
            yield break;

        Quaternion rotation = visual.localRotation;
        Vector3 scale = visual.localScale;
        float elapsed = 0f;
        while (elapsed < deathDuration)
        {
            float t = Mathf.Clamp01(elapsed / deathDuration);
            visual.localRotation = rotation * Quaternion.Euler(-65f * t, 0f, 15f * t);
            visual.localScale = scale * Mathf.Lerp(1f, 0.3f, t * t);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        visual.localRotation = rotation * Quaternion.Euler(-65f, 0f, 15f);
        visual.localScale = scale * 0.3f;
    }

    private void Update()
    {
        if (!health.IsAlive)
            return;

        Vector2 input = joystick != null ? joystick.Input.normalized : Vector2.zero;
        Vector3 direction = new Vector3(input.x, 0f, input.y);
        Vector3 previousPosition = transform.position;
        Vector3 position = transform.position + direction * (movementSpeed * Time.deltaTime);
        position = ClampToArena(position);
        transform.position = position;
        if (animator != null)
            animator.SetFloat("Speed", Time.deltaTime > 0f ? (position - previousPosition).magnitude / Time.deltaTime : 0f);

        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    public Vector3 ClampToArena(Vector3 position)
    {
        Vector3 center = arenaCenter != null ? arenaCenter.position : Vector3.zero;
        position.x = Mathf.Clamp(position.x, center.x - Mathf.Abs(arenaHalfExtents.x), center.x + Mathf.Abs(arenaHalfExtents.x));
        position.z = Mathf.Clamp(position.z, center.z - Mathf.Abs(arenaHalfExtents.y), center.z + Mathf.Abs(arenaHalfExtents.y));
        return position;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = arenaCenter != null ? arenaCenter.position : Vector3.zero;
        center.y = transform.position.y;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, new Vector3(Mathf.Abs(arenaHalfExtents.x) * 2f, 0.1f,
            Mathf.Abs(arenaHalfExtents.y) * 2f));
    }
}
