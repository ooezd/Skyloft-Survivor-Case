using UnityEngine;

[RequireComponent(typeof(Canvas))]
public sealed class DamageNumberCanvas : MonoBehaviour
{
    [SerializeField] private Camera worldCamera;
    [SerializeField] private DamageNumber numberPrefab;
    [Tooltip("Offset from the enemy's aim position in world units.")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.9f, 0f);

    private static DamageNumberCanvas instance;
    private Canvas canvas;

    private void OnEnable()
    {
        canvas = GetComponent<Canvas>();
        instance = this;
    }

    private void OnDisable()
    {
        if (instance == this)
            instance = null;
    }

    public static void Show(Vector3 position, int damage)
    {
        if (instance == null || damage <= 0)
            return;
        instance.Spawn(position, damage);
    }

    private void Spawn(Vector3 position, int damage)
    {
        if (numberPrefab == null || worldCamera == null)
        {
            Debug.LogError("Assign the damage number prefab and world camera on Damage Number Canvas.", this);
            return;
        }

        DamageNumber number = Instantiate(numberPrefab, transform, false);
        number.Initialize(damage, position + worldOffset, worldCamera, canvas);
    }
}
