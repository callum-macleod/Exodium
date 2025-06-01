using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkSpawner : NetworkBehaviour
{
    public static NetworkSpawner Instance;

    public NetworkObject triflePrefab;
    public NetworkObject spudPrefab;

    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
    }

    public void Test(object sender, HostStartedEventArgs e)
    {
        Debug.LogWarning("spawning shit");
        NetworkManager?.SpawnManager.InstantiateAndSpawn(triflePrefab, OwnerClientId);
        NetworkManager?.SpawnManager.InstantiateAndSpawn(spudPrefab, OwnerClientId);
    }
}
