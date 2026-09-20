using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 18f, -12f);
    [SerializeField] private float lookHeight = 0.8f;

    private void LateUpdate()
    {
        if (target == null)
            return;

        transform.position = target.position + offset;
        transform.LookAt(target.position + Vector3.up * lookHeight);
    }
}
