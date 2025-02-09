using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TRifle : WeaponBase
{
    protected override int baseDamage { get; set; } = 44;
    protected override float critMultiplier { get; set; } = 3f;
    protected override float limbMultiplier { get; set; } = 0.8f;

    protected override void OnFixedUpdate()
    {
        
    }

    protected override void OnStart()
    {
        
    }

    protected override void OnUpdate()
    {
        
    }

    public override void Shoot()
    {
        print($"{name}: Shooting");
    }
}