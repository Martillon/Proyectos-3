using UnityEngine;
using Scripts.Player.Weapons.Interfaces;

namespace Scripts.Player.Weapons.Pickups
{
    /// <summary>
    /// UpgradePickup
    /// 
    /// Represents a collectible weapon upgrade in the scene.
    /// When the player touches it, the weapon is updated to the contained upgrade.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class UpgradePickup : MonoBehaviour, IWeaponPickup
    {
        [SerializeField] private MonoBehaviour upgradeScript; // Must implement IWeaponUpgrade

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            WeaponBase weapon = other.GetComponentInChildren<WeaponBase>();
            if (weapon == null) return;

            IWeaponUpgrade upgrade = GetUpgrade();
            if (upgrade != null)
            {
                weapon.SetUpgrade(upgrade);
                Destroy(gameObject); // Pickup consumed
            }
        }

        public IWeaponUpgrade GetUpgrade()
        {
            return upgradeScript as IWeaponUpgrade;
        }
    }
}
