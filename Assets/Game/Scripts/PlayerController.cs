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
    [SerializeField] private DeathAnimation deathAnimation;

    private Health health;

    private void Awake() => health = GetComponent<Health>();

    private void OnEnable() => health.DamageReceived += ShowDamageNumber;

    private void ShowDamageNumber(Health damagedHealth, int damage) =>
        DamageNumberCanvas.ShowPlayerDamage(damagedHealth.AimPosition, damage);

    private void OnDisable()
    {
        health.DamageReceived -= ShowDamageNumber;
        if (animator != null)
            animator.SetFloat("Speed", 0f);
    }

    public System.Collections.IEnumerator ShowDeath()
    {
        if (deathAnimation != null)
            yield return deathAnimation.Play();
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
