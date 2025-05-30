using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// To Do:
// 1) units can in theory enter the trigger multiple times, even in the short time it exists. Probs fix this.

public class ExplosionScript : MonoBehaviour
{
    [SerializeField] private float baseDamage = 90f;

    private void Awake()
    {
        // Explosion should only exist for short time, destroy it after that time.
        Invoke(nameof(DestroyMe), 0.05f);
    }

    private void OnTriggerEnter(Collider collider)
    {
        // Deal damage to every unit in range of collider.
        collider.GetComponent<HealthManager>().ApplyDamage((int)baseDamage);
    }

    void DestroyMe()
    {
        Destroy(gameObject);
    }
}
