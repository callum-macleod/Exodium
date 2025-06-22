using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BulletHole : NetworkBehaviour
{
    float spawnTime;
    float duration = 5f;

    public override void OnNetworkSpawn()
    {
        spawnTime = Time.time;
    }

    private void Update()
    {
        if (Time.time - spawnTime > duration)
            DespawnRpc();
    }

    [Rpc(SendTo.Server)]
    private void DespawnRpc()
    {
        NetworkObject.Despawn();
    }
}
