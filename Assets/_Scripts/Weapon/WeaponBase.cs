using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public abstract class WeaponBase : NetworkBehaviour
{
    public int weaponID;

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

    private bool isDetached = false;
    //private NetworkVariable<bool> isDetached = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    //!!!! THIS IS NOT NETWORKED BECAUSE IT USES A CUSTOM SETTER
    // IF YOU NEED TO CONVERT TO NETWORK VARIABLE, REPLACE THE CUSTOM SETTER WITH NetworkVariable.OnValueChanged!!!!

    public bool IsDetached {
        get { return isDetached; }
        set
        {
            // if detaching: reset attached tanc
            if (value)
                AttachedTanc = null;

            // you must set the attached tanc before setting IsDetached - ensures that you aren't attaching unexpectedly 
            else if (AttachedTanc == null)
                throw new NullReferenceException();

            if (_collider != null) _collider.enabled = value;
            if (_rigidbody != null) _rigidbody.isKinematic = !value;

            isDetached = value;
        }
    }

    public bool StartAsDetached = false;

    void Start() => OnStart();
    protected virtual void OnStart()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();

        if (StartAsDetached)
            IsDetached = true;

        AttachedTancNetObjID.OnValueChanged += OnAttachedTancNetObjIDChanged;
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

        AttachedTanc?.Attach(gameObject, weaponID, WeaponSlot);
    }

    void Update() => OnUpdate();
    protected virtual void OnUpdate() { }

    void FixedUpdate() => OnFixedUpdate();
    protected virtual void OnFixedUpdate() { }


    public abstract void Shoot();

    private void OnCollisionEnter(Collision collision)
    {
        if (collision != null && collision.gameObject.layer == (int)Layers.SolidGround)
            _rigidbody.isKinematic = true;
    }
}
