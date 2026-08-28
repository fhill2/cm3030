using UnityEngine;

namespace Game.Combat
{
    public class ThrowCooldownCheat : MonoBehaviour
    {
        private void Start()
        {
            WeaponThrower thrower = FindFirstObjectByType<WeaponThrower>();
            if (thrower == null)
            {
                Debug.LogWarning("[ThrowCooldownCheat] No WeaponThrower in the scene, cooldown not removed.");
                return;
            }

            thrower.SetCooldown(0f);
            Debug.Log("[ThrowCooldownCheat] Throw cooldown removed.");
        }
    }
}
