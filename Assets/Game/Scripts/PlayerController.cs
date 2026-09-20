using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private VirtualJoystick joystick;
    [SerializeField, Min(0f)] private float movementSpeed = 5f;
    [SerializeField] private Vector2 arenaHalfExtents = new Vector2(18f, 18f);

    private void Update()
    {
        Vector2 input = joystick != null ? Vector2.ClampMagnitude(joystick.Input, 1f) : Vector2.zero;
        Vector3 direction = new Vector3(input.x, 0f, input.y);
        Vector3 position = transform.position + direction * (movementSpeed * Time.deltaTime);
        position.x = Mathf.Clamp(position.x, -Mathf.Abs(arenaHalfExtents.x), Mathf.Abs(arenaHalfExtents.x));
        position.z = Mathf.Clamp(position.z, -Mathf.Abs(arenaHalfExtents.y), Mathf.Abs(arenaHalfExtents.y));
        transform.position = position;

        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(direction);
    }
}
