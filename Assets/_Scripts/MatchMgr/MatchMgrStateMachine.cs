using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchMgrStateMachine : StateMachineSAuth
{
    [SerializeField] float RoundDuration;

    MatchMgr matchMgr;
    public MatchMgr MatchMgr
    {
        get
        {
            if (matchMgr == null)
                matchMgr = GetComponent<MatchMgr>();
            return matchMgr;
        }
    }
    public float GetRoundDuration() { return RoundDuration; }

    // Start is called before the first frame update
    void Start()
    {
        states = new List<State>() { new RoundPhaseState(this)};
    }
}
