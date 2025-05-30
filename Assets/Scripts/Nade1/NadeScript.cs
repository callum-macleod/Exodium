using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NadeScript : NetworkBehaviour
{
    [SerializeField] private NetworkObject explosion;


    private void Start()
    {
        GetComponent<Rigidbody>().velocity = transform.forward * 10f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        NetworkObject explosionTemp = NetworkManager.SpawnManager.InstantiateAndSpawn(explosion, OwnerClientId);

        explosionTemp.GetComponent<NadeScript>().InitializeTransformRpc(transform.position, transform.rotation);

        Destroy(this.gameObject);
    }

    [Rpc(SendTo.Everyone)]
    public void InitializeTransformRpc(Vector3 pos, Quaternion rot)
    {
        transform.position = pos;
        transform.rotation = rot;
    }
}
