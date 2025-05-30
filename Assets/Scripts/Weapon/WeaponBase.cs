using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public abstract class WeaponBase : NetworkBehaviour
{
    // weapon stats
    protected abstract int baseDamage { get; set; }
    protected abstract float critMultiplier { get; set; }
    protected abstract float limbMultiplier { get; set; }
    public abstract WeaponSlot WeaponSlot { get; }


    // behaviour while detached from a tanc (i.e. on the floor)
    [SerializeField] Rigidbody rigidbody { get; set; }
    [SerializeField] Collider collider { get; set; }


    [NonSerialized] public Tanc? AttachedTanc;

    private NetworkVariable<bool> isDetached = new NetworkVariable<bool>(false);

    public bool IsDetached {
        get { return isDetached.Value; }
        set
        {
            // if detaching: reset attached tanc
            if (value)
                AttachedTanc = null;

            // you must set the attached tanc before setting IsDetached - ensures that you aren't attaching unexpectedly 
            else if (AttachedTanc == null)
                throw new NullReferenceException();

            if (collider != null) collider.enabled = value;
            if (rigidbody != null) rigidbody.isKinematic = !value;
            isDetached.Value = value;
        }
    }

    public bool StartAsDetached = false;

    void Start() => OnStart();
    protected virtual void OnStart()
    {
        rigidbody = GetComponent<Rigidbody>();
        collider = GetComponent<Collider>();

        if (StartAsDetached)
            IsDetached = true;
    }

    void Update() => OnUpdate();
    protected virtual void OnUpdate() { }

    void FixedUpdate() => OnFixedUpdate();
    protected virtual void OnFixedUpdate() { }


    public abstract void Shoot();

    private void OnCollisionEnter(Collision collision)
    {
        if (collision != null && collision.gameObject.layer == (int)Layers.SolidGround)
            rigidbody.isKinematic = true;
    }
}
