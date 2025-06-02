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
        SpawnC2SRpc(Weapons.TRifle, (Vector3.left + Vector3.up) * 2);
        SpawnC2SRpc(Weapons.Spud, (Vector3.right + Vector3.up) * 2);
    }

    [Rpc(SendTo.Server)]
    public void SpawnC2SRpc(Weapons lookupID, Vector3 position)
    {
        NetworkObject trifleNetObj = NetworkManager.SpawnManager.InstantiateAndSpawn(weaponLookup.Dict[lookupID], OwnerClientId).GetComponent<NetworkObject>();

        SpawnS2CRpc(new NetworkObjectReference(trifleNetObj), position);
    }

    [Rpc(SendTo.Everyone)]
    private void SpawnS2CRpc(NetworkObjectReference netObjRef, Vector3 position)
    {
        netObjRef.TryGet(out NetworkObject networkObj);

        networkObj.transform.position = position;
        networkObj.GetComponent<WeaponBase>().IsDetached = true;
    }
}
