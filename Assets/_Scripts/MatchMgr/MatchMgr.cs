using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
using System.Linq;

public class MatchMgr : NetworkBehaviour
{
    MatchMgrStateMachine stateMachine;
    public MatchMgrStateMachine StateMachine
    {
        get
        {
            if (stateMachine == null)
                stateMachine = GetComponent<MatchMgrStateMachine>();
            return stateMachine;
        }
    }

    [SerializeField] List<Rebel> rebels = new List<Rebel>();
    public bool RecievedLocalRebel { get; private set; } = false;

    public static MatchMgr Instance;
    private void Start() { Instance = this; }

    public override void OnNetworkSpawn()
    {
        if (!RecievedLocalRebel && ClientSideMgr.Instance.ClientOwnedRebel != null)
            RegisterRebel(ClientSideMgr.Instance.ClientOwnedRebel.GetComponent<Rebel>());
    }

    // Update is called once per frame
    void Update()
    {
        if (IsServer) ServerUpdate();
        if (IsOwner) OwnerUpdate();
    }

    private void ServerUpdate()
    {
        if (StateMachine.GetCurrentState()?.GetType() == typeof(RoundPhaseState))
        {
            RoundPhaseState currState = (RoundPhaseState)StateMachine.GetCurrentState();
            GetClientRebel().RoundTimerUI.text = currState.RemainingRoundTime.ToString();
        }
    }

    private void OwnerUpdate()
    {

    }

    Rebel GetClientRebel()
    {
        return ClientSideMgr.Instance.ClientOwnedRebel.GetComponent<Rebel>();
    }

    public void RegisterRebel(Rebel rebel)
    {
        print($"{{LOCAL}} OCID: {OwnerClientId} => Registering Rebel {rebel.NetworkObjectId}");
        // if is client: inform server of new player
        if (!IsServer && IsSpawned)
        {
            RegisterRebelRpc(new NetworkObjectReference(rebel.NetworkObject));
        }
        else
        {
            // if not yet spawned OR is server: add rebel and update clients
            rebels.Add(rebel);

            // send rpc to clients
            if (IsSpawned) UpdateClientRebelList();
        }

        RecievedLocalRebel = true;
    }

    [Rpc(SendTo.Server)]
    public void RegisterRebelRpc(NetworkObjectReference nObjRef)
    {
        nObjRef.TryGet(out NetworkObject nObj);
        Rebel t = nObj.GetComponent<Rebel>();
        print($"{{SRPC}} OCID: {OwnerClientId} => Recieving Rebel {t.NetworkObjectId} for registration");
        rebels.Add(t);

        UpdateClientRebelList();
    }

    void UpdateClientRebelList()
    {
        print($"{{LOCAL}} OCID: {OwnerClientId} => Sending updated Rebel list to clients");


        List<NetworkObjectReference> nObjRefs = new();
        foreach (Rebel t in rebels)
        {
            NetworkObjectReference n = new NetworkObjectReference(t.NetworkObject);
            nObjRefs.Add(new NetworkObjectReference(t.NetworkObject));
        }

        UpdateClientRebelListRpc(nObjRefs.ToArray());
    }

    // server updating clients' rebel lists
    [Rpc(SendTo.NotServer)]
    public void UpdateClientRebelListRpc(NetworkObjectReference[] nObjRefs)
    {
        print($"{{NSRPC}} OCID: {OwnerClientId} => Recieving updated Rebel list from server");
        List<Rebel> t = new();
        foreach (NetworkObjectReference nor in nObjRefs)
        {
            nor.TryGet(out NetworkObject nObj);
            t.Add(nObj.GetComponent<Rebel>());
        }

        rebels = t;
    }
}
