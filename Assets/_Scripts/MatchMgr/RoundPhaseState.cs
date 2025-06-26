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

        if (GetParent().GetCurrentState()?.GetType() == typeof(RoundPhaseState))
        {
            RoundPhaseState currState = (RoundPhaseState)GetParent().GetCurrentState();
            GetClientRebel().RoundTimerUI.text = currState.RemainingRoundTime.ToString();
        }
    }

    public void StartRound()
    {
        RoundStartTime = Time.time;
    }

    public void EndRound()
    {
        Reset();
    }

    public override void Reset()
    {
        StartRound();
    }

    Rebel GetClientRebel()
    {
        return ClientSideMgr.Instance.ClientOwnedRebel.GetComponent<Rebel>();
    }
}
