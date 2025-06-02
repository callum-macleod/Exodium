using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public abstract class WeaponBase : NetworkBehaviour
{
    public Weapons weaponID;

    // weapon stats
    protected abstract int baseDamage { get; set; }
    protected abstract float critMultiplier { get; set; }
    protected abstract float limbMultiplier { get; set; }
    public abstract WeaponSlot WeaponSlot { get; }


    // behaviour while detached from a tanc (i.e. on the floor)
    [SerializeField] Rigidbody _rigidbody { get; set; }
    [SerializeField] Collider _collider { get; set; }


    [SerializeField] public Tanc AttachedTanc;
    public NetworkVariable<ulong> AttachedTancNetObjID { get; set; } = new NetworkVariable<ulong>();

    //private bool isDetached = false;
    public NetworkVariable<bool> IsDetached { get; private set; } = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public void SetIsDetachedIfOwner(bool val)
    {
        if (IsOwner) IsDetached.Value = val;
    }
    //public bool IsDetached {
    //    get { return isDetached; }
    //    set
    //    {
    //        // if detaching: reset attached tanc
    //        if (value)
    //            AttachedTanc = null;

    //        //// you must set the attached tanc before setting IsDetached - ensures that you aren't attaching unexpectedly 
    //        //else if (AttachedTanc == null)
    //        //    throw new NullReferenceException();

    //        if (_collider != null) _collider.enabled = value;
    //        if (_rigidbody != null) _rigidbody.isKinematic = !value;

    //        isDetached = value;
    //    }
    //}

    public bool StartAsDetached = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();


        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();

        AttachedTancNetObjID.OnValueChanged += OnAttachedTancNetObjIDChanged;
        IsDetached.OnValueChanged += OnIsDetachedChanged;

        SetIsDetachedIfOwner(StartAsDetached);
    }


    private void OnAttachedTancNetObjIDChanged(ulong prev, ulong curr)
    {
        // find attached tank using associated network object id
        foreach (Tanc tanc in FindObjectsOfType(typeof(Tanc)))
        {
            if (tanc.GetComponent<NetworkObject>().NetworkObjectId == AttachedTancNetObjID.Value)
            {
                AttachedTanc = tanc;
                break;
            }
        }

        AttachedTanc?.Attach(gameObject, (int)weaponID, WeaponSlot);
    }

    void Update() => OnUpdate();
    protected virtual void OnUpdate() { }

    void FixedUpdate() => OnFixedUpdate();
    protected virtual void OnFixedUpdate() { }

    private void OnIsDetachedChanged(bool prev,  bool curr)
    {
        // if detaching: reset attached tanc
        if (curr) AttachedTanc = null;

        if (_collider != null) _collider.enabled = curr;
        if (_rigidbody != null) _rigidbody.isKinematic = !curr;
    }


    public abstract void Shoot();

    private void OnCollisionEnter(Collision collision)
    {
        if (collision != null && collision.gameObject.layer == (int)Layers.SolidGround)
            _rigidbody.isKinematic = true;
    }
}
