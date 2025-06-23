using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Package : WeaponBase
{
    public override WeaponSlot WeaponSlot => WeaponSlot.Package;

    public override float MaxVelocity => 9;

    protected override int baseDamage { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    protected override float critMultiplier { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    protected override float limbMultiplier { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public override void Shoot()
    {
        throw new System.NotImplementedException();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
