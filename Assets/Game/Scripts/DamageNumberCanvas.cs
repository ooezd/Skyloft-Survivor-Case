using UnityEngine;

[RequireComponent(typeof(Canvas))]
public sealed class DamageNumberCanvas : MonoBehaviour
{
    [SerializeField] private Camera worldCamera;
    [SerializeField] private DamageNumber numberPrefab;
    [SerializeField] private DamageNumber playerDamagePrefab;
    [SerializeField] private DamageNumber upgradeTextPrefab;
    [Tooltip("Offset from the character's aim position in world units.")]
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
        instance.Spawn(instance.numberPrefab, position, damage);
    }

    public static void ShowPlayerDamage(Vector3 position, int damage)
    {
        if (instance == null || damage <= 0)
            return;
        instance.Spawn(instance.playerDamagePrefab, position, damage);
    }

    public static void ShowUpgrade(Vector3 position, string message)
    {
        if (instance == null || string.IsNullOrEmpty(message) || instance.worldCamera == null)
            return;
        DamageNumber prefab = instance.upgradeTextPrefab != null ? instance.upgradeTextPrefab : instance.numberPrefab;
        if (prefab == null)
            return;
        DamageNumber number = Instantiate(prefab, instance.transform, false);
        number.Initialize(message, position + instance.worldOffset, instance.worldCamera, instance.canvas);
    }

    private void Spawn(DamageNumber prefab, Vector3 position, int damage)
    {
        if (prefab == null || worldCamera == null)
        {
            Debug.LogError("Assign the damage number prefab and world camera on Damage Number Canvas.", this);
            return;
        }

        DamageNumber number = Instantiate(prefab, transform, false);
        number.Initialize(damage, position + worldOffset, worldCamera, canvas);
    }
}
