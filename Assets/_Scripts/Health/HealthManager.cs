using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine.Rendering;

public class HealthManager : NetworkBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private NetworkVariable<float> currentHealth;

    [SerializeField] private Slider healthSlider;
    [SerializeField] private DeathOptions DeathOption = DeathOptions.SetInactive;

    public override void OnNetworkSpawn()
    {
        currentHealth.OnValueChanged += UpdateHealthBar;

        healthSlider.maxValue = maxHealth;
        currentHealth.Value = maxHealth;
        healthSlider.value = currentHealth.Value;
    }


    private void UpdateHealthBar(float previous, float current)
    {
        healthSlider.value = current;
    }


    [Rpc(SendTo.Server)]
    public void ApplyDamageRpc(float _dmg)
    {
        if (_dmg <= 0) return;

        currentHealth.Value -= _dmg;

        if (currentHealth.Value <= 0)
        {
            if (TryGetComponent(out Rebel rebel))
            {
                rebel.DropWeaponRpc(WeaponSlot.Package);
                rebel.DropHighestWeaponRpc();
            }

            //Invoke(nameof(Die), 0.5f);

            if (DeathOption == DeathOptions.SetInactive) SetInactiveRpc();
            if (DeathOption == DeathOptions.Despawn) NetworkObject.Despawn();
        }
    }

    void Die()
    {
        if (DeathOption == DeathOptions.SetInactive) SetInactiveRpc();
        if (DeathOption == DeathOptions.Despawn) NetworkObject.Despawn();
    }

    [Rpc(SendTo.Everyone)]
    public void SetInactiveRpc() { gameObject.SetActive(false); }


    [Rpc(SendTo.Server)]
    public void ApplyHealRpc(float _heal)
    {
        if (_heal <= 0) return;

        currentHealth.Value += _heal;

        if (currentHealth.Value >= maxHealth)
            currentHealth.Value = maxHealth;
    }
}
