using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundPhaseState : State
{
    public float RoundStartTime { get; private set; }

    float roundDuration { get { return GetParent().GetRoundDuration(); } }
    public float RemainingRoundTime { get { return roundDuration - CurrentRoundTime; } }
    public float CurrentRoundTime { get { return Time.time - RoundStartTime; } }

    public RoundPhaseState(StateMachineSAuth parentMachine) : base(parentMachine) { }

    public MatchMgrStateMachine GetParent() { return (MatchMgrStateMachine)parent; }

    public override void Act()
    {
        if (RemainingRoundTime <= 0) EndRound();
    }

    public void StartRound()
    {
        RoundStartTime = Time.time;
        // tell everyone the rouds started
        GetParent().MatchMgr.OnRoundStartRpc();
    }

    public void EndRound()
    {
        Reset();
    }

    public override void Reset()
    {
        StartRound();
    }
}
