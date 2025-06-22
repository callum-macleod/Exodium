using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitboxScript : MonoBehaviour
{
    [SerializeField] private HealthManager healthManager;

    [SerializeField] private float multiplier = 1;

    public void DealDamage(float _val)
    {
        healthManager.ApplyDamageRpc(_val * multiplier);
    }
}
