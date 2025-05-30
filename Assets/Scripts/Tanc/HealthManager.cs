using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthManager : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void ApplyDamage(float _dmg)
    {
        if (_dmg <= 0) return;

        currentHealth -= _dmg;

        if (currentHealth <= 0)
            Destroy(this.gameObject);
    }

    public void ApplyHeal(float _heal)
    {
        if (_heal <= 0) return;

        currentHealth += _heal;

        if (currentHealth >= maxHealth)
            currentHealth = maxHealth;
    }
}
