using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class HealthManager : NetworkBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private NetworkVariable<float> currentHealth;

    [SerializeField] private Slider healthSlider;

    public override void OnNetworkSpawn()
    {
        currentHealth.OnValueChanged += UpdateHealthBar;

        healthSlider.maxValue = maxHealth;
        currentHealth.Value = maxHealth;
        healthSlider.value = currentHealth.Value;
    }


    private void UpdateHealthBar(float previous, float current)
    {
        healthSlider.value = current; // 
    }


    public void ApplyDamage(float _dmg)
    {
        if (_dmg <= 0) return;

        currentHealth.Value -= _dmg;

        if (currentHealth.Value <= 0)
            Destroy(this.gameObject);
    }


    public void ApplyHeal(float _heal)
    {
        if (_heal <= 0) return;

        currentHealth.Value += _heal;

        if (currentHealth.Value >= maxHealth)
            currentHealth.Value = maxHealth;
    }
}
