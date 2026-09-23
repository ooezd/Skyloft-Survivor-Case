using UnityEngine;

[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
public sealed class UpgradePickup : MonoBehaviour
{
    private UpgradePickupSpawner owner;
    private bool collected;

    public void Initialize(UpgradePickupSpawner spawner, float lifetime)
    {
        owner = spawner;
        collected = false;
        Destroy(gameObject, Mathf.Max(0.1f, lifetime));
    }

    private void OnTriggerEnter(Collider other) => TryCollect(other.GetComponentInParent<Health>());

    public bool TryCollect(Health collector)
    {
        if (collected || owner == null || !owner.CanCollect(collector))
            return false;

        // Latch before granting the reward: multiple player colliders cannot collect twice.
        collected = true;
        bool damageUpgrade = Random.Range(0, 2) == 0;
        owner.PlayerAttack.ApplyRunUpgrade(damageUpgrade);
        DamageNumberCanvas.ShowUpgrade(collector.AimPosition,
            damageUpgrade ? "DAMAGE +10%" : "ATTACK FREQUENCY +10%");
        gameObject.SetActive(false);
        Destroy(gameObject);
        return true;
    }
}
