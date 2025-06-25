using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

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

    public override void OnNetworkSpawn()
    {
        gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (IsServer) ServerUpdate();
        if (IsOwner) OwnerUpdate();
    }

    private void ServerUpdate()
    {
        if (StateMachine.GetCurrentState().GetType() == typeof(RoundPhaseState))
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

    ClientSideMgr GetClientSideMgr()
    {
        return GameObject.FindGameObjectWithTag("ClientSideMgr").GetComponent<ClientSideMgr>();
    }

    Tanc GetClientTanc()
    {
        return GetClientSideMgr().ClientOwnedTanc.GetComponent<Tanc>();
    }
}
