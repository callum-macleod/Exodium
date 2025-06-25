using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;


public abstract class State
{
    protected StateMachineSAuth parent;

    public State(StateMachineSAuth parentMachine)
    {
        parent = parentMachine;
    }


    // do action associated with state (called from FixedUpdate)
    public abstract void Act();

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        throw new NotImplementedException();
    }

    // called when returning to a state (does nothing by default)
    public virtual void Reset() { }
}