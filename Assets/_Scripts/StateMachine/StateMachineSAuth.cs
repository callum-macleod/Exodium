using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using JetBrains.Annotations;
using Unity.VisualScripting.Antlr3.Runtime.Misc;


/// <summary>
/// Finite State Machine
/// </summary>
public class StateMachineSAuth : NetworkBehaviour
{
    protected State currentState;

    // this acts as a list of states and as a state pool for the machine
    public virtual List<State> states { get; protected set; }

    // used to set current state to a state from a state in the state pool
    public void UpdateCurrentState(Type newStateType)
    {
        if (!IsServer) return;

        if (states == null)
            Debug.Log($"{typeof(StateMachineSAuth)}.{nameof(states)} was null");

        // get state of matching type from pool
        State stateFromPool = states.FirstOrDefault(s => s.GetType() == newStateType);
        if (stateFromPool == null)
            Debug.LogError($"Cannot find State of type {newStateType} in {typeof(StateMachineSAuth)}.{nameof(states)}");

        //if (currentState != null)
        //    print($"Swapping current state from {currentState.GetType()} to {newStateType}");
        //else
        //    print($"Setting current state from to {newStateType}");

        //currentState = stateFromPool;
        //currentState.Reset();
        UpdateCurrentStateRpc(states.IndexOf(stateFromPool));
    }

    [Rpc(SendTo.Everyone)]
    void UpdateCurrentStateRpc(int stateIndex)
    {
        currentState = states[stateIndex];
        currentState.Reset();
    }

    private void FixedUpdate()
    {
        if (currentState != null) currentState.Act();
    }

    public State GetCurrentState() { return currentState; }
}