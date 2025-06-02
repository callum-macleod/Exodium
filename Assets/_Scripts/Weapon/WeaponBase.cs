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
    public NetworkVariable<NetworkObjectReference> AttachedTancNetObjRef { get; set; } = new NetworkVariable<NetworkObjectReference>();

    //private bool isDetached = false;
    public NetworkVariable<bool> IsDetached { get; private set; } = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public void SetIsDetachedIfOwner(bool val)
    {
        if (IsOwner) IsDetached.Value = val;
    }


    public override void OnNetworkSpawn()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();

        AttachedTancNetObjRef.OnValueChanged += OnAttachedTancNetObjIDChanged;
        IsDetached.OnValueChanged += OnIsDetachedChanged;
    }


    private void OnAttachedTancNetObjIDChanged(NetworkObjectReference prev, NetworkObjectReference curr)
    {
        curr.TryGet(out NetworkObject t);
        AttachedTanc = t.GetComponent<Tanc>();

        AttachedTanc.Attach(gameObject, weaponID, WeaponSlot);
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
