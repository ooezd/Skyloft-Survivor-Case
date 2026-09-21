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

    private Health health;

    private void Awake() => health = GetComponent<Health>();

    private void OnEnable() => health.Died += HandleDeath;

    private void OnDisable() => health.Died -= HandleDeath;

    private void HandleDeath(Health deadHealth)
    {
        if (visual != null)
            visual.localRotation *= Quaternion.Euler(0f, 0f, 90f);
    }

    private void Update()
    {
        if (!health.IsAlive)
            return;

        Vector2 input = joystick != null ? joystick.Input.normalized : Vector2.zero;
        Vector3 direction = new Vector3(input.x, 0f, input.y);
        Vector3 position = transform.position + direction * (movementSpeed * Time.deltaTime);
        Vector3 center = arenaCenter != null ? arenaCenter.position : Vector3.zero;
        position.x = Mathf.Clamp(position.x, center.x - Mathf.Abs(arenaHalfExtents.x), center.x + Mathf.Abs(arenaHalfExtents.x));
        position.z = Mathf.Clamp(position.z, center.z - Mathf.Abs(arenaHalfExtents.y), center.z + Mathf.Abs(arenaHalfExtents.y));
        transform.position = position;

        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(direction);
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
