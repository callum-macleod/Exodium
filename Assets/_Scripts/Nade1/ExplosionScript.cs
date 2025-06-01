using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// To Do:
// 1) units can in theory enter the trigger multiple times, even in the short time it exists. Probs fix this.

public class ExplosionScript : MonoBehaviour
{
    [SerializeField] private float baseDamage = 90f;

    private void OnTriggerEnter(Collider collider)
    {
        // Deal damage to every unit in range of collider.
        collider.GetComponent<HealthManager>().ApplyDamageRpc((int)baseDamage);
    }

    [Rpc(SendTo.Everyone)]
    public void InitializeTransformRpc(Vector3 pos, Quaternion rot)
    {
        transform.position = pos;
        transform.rotation = rot;
    }
}
