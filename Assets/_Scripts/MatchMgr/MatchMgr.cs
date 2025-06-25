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

    [SerializeField] List<Tanc> tancs = new List<Tanc>();
    public bool RecievedLocalTanc { get; private set; } = false;

    public static MatchMgr Instance;
    private void Start() { Instance = this; }

    public override void OnNetworkSpawn()
    {
        if (!RecievedLocalTanc && ClientSideMgr.Instance.ClientOwnedTanc != null)
            RegisterTanc(ClientSideMgr.Instance.ClientOwnedTanc.GetComponent<Tanc>());
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
            GetClientTanc().RoundTimerUI.text = currState.RemainingRoundTime.ToString();

            //if (RemainingRoundTime > 10 )
            //if (RemainingRoundTime % 10 < 0.1)
            // send pulse to clients to make sure their timers are synced
            // set cooldown for next pulse ( could be 0.5s)
            //else if (RemainingRoundTime > 1)
            //if (RemainingRoundTime % 1 < 0.1)
            // send pulse to clients to make sure their timers are synced
            // set cooldown for next pulse ( could be 0.5s)
            //else (RemainingRoundTime > 0)
            // send pulse to clients to make sure their timers are synced
            // JUST DO IT EVERY FRAME - NO COOLDOWN
        }
    }

    private void OwnerUpdate()
    {

    }

    [Rpc(SendTo.Everyone)]
    public void OnRoundStartRpc()
    {
        //GetClientTanc().RoundTimerUI;
    }

    Tanc GetClientTanc()
    {
        return ClientSideMgr.Instance.ClientOwnedTanc.GetComponent<Tanc>();
    }

    public void RegisterTanc(Tanc tanc)
    {
        print($"{{LOCAL}} NOID: {NetworkObjectId} => Registering Tanc {tanc.NetworkObjectId}");
        // if is client: inform server of new player
        if (!IsServer && IsSpawned)
        {
            RegisterTancRpc(new NetworkObjectReference(tanc.NetworkObject));
        }
        else
        {
            // if not yet spawned OR is server: add tanc and update clients
            tancs.Add(tanc);

            // send rpc to clients
            if (IsSpawned) UpdateClientTancList();
        }

        RecievedLocalTanc = true;
    }

    [Rpc(SendTo.Server)]
    public void RegisterTancRpc(NetworkObjectReference nObjRef)
    {
        nObjRef.TryGet(out NetworkObject nObj);
        Tanc t = nObj.GetComponent<Tanc>();
        print($"{{SRPC}} NOID: {NetworkObjectId} => Recieving tanc {t.NetworkObjectId} for registration");
        tancs.Add(t);

        UpdateClientTancList();
    }

    void UpdateClientTancList()
    {
        print($"{{LOCAL}} NOID: {NetworkObjectId} => Sending updated Tanc list to clients");


        List<NetworkObjectReference> nObjRefs = new();
        foreach (Tanc t in tancs)
        {
            NetworkObjectReference n = new NetworkObjectReference(t.NetworkObject);
            nObjRefs.Add(new NetworkObjectReference(t.NetworkObject));
        }

        //NetworkObjectReference[] nObjRefs = new NetworkObjectReference[] { };
        //foreach (Tanc t in tancs)
        //{
        //    NetworkObjectReference n = new NetworkObjectReference(t.NetworkObject);
        //    nObjRefs.Append(new NetworkObjectReference(t.NetworkObject));
        //}

        UpdateClientTancListRpc(nObjRefs.ToArray());
    }

    // server updating clients' tanc lists
    [Rpc(SendTo.NotServer)]
    public void UpdateClientTancListRpc(NetworkObjectReference[] nObjRefs)
    {
        print($"{{NSRPC}} NOID: {NetworkObjectId} => Recieving updated Tanc list from server");
        List<Tanc> t = new();
        foreach (NetworkObjectReference nor in nObjRefs)
        {
            nor.TryGet(out NetworkObject nObj);
            t.Add(nObj.GetComponent<Tanc>());
        }

        tancs = t;
    }
}
