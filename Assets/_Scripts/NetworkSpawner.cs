using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkSpawner : NetworkBehaviour
{
    public static NetworkSpawner Instance;

    public NetworkObject triflePrefab;
    public NetworkObject spudPrefab;

    [SerializeField] WeaponLookupSO weaponLookup;


    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
    }

    public void Test(object sender, HostStartedEventArgs e)
    {
        Debug.LogWarning("spawning shit");
        NetworkManager.SpawnManager.InstantiateAndSpawn(weaponLookup.Dict[2], OwnerClientId).gameObject.transform.position += Vector3.up;
        NetworkManager.SpawnManager.InstantiateAndSpawn(weaponLookup.Dict[1], OwnerClientId).gameObject.transform.position += Vector3.up;
        //NetworkManager?.SpawnManager.InstantiateAndSpawn(triflePrefab, OwnerClientId);
        //NetworkManager?.SpawnManager.InstantiateAndSpawn(spudPrefab, OwnerClientId);
    }
}
