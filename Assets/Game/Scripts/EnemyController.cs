using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField, Min(0f)] private float movementSpeed = 1.8f;

    public void SetTarget(Transform player) => target = player;

    private void Update()
    {
        if (target == null)
            return;

        Vector3 destination = new Vector3(target.position.x, transform.position.y, target.position.z);
        Vector3 direction = destination - transform.position;
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(direction);
        transform.position = Vector3.MoveTowards(transform.position, destination, movementSpeed * Time.deltaTime);
    }
}
