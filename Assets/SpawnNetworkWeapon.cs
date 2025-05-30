using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SpawnNetworkWeapon : NetworkBehaviour
{
    public NetworkObject weapon;

    // Start is called before the first frame update
    void Awake()
    {
        NetworkManager.SpawnManager.InstantiateAndSpawn(weapon, OwnerClientId);
    }
}
