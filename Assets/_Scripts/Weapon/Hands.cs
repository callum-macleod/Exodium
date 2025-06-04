using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hands : WeaponBase
{
    public override WeaponSlot WeaponSlot { get; } = WeaponSlot.Melee;

    public override float MaxVelocity { get; } = 20;

    protected override int baseDamage { get; set; } = 30;
    protected override float critMultiplier { get; set; } = 2f;
    protected override float limbMultiplier { get; set; } = 1f;

    public override void Shoot()
    {

    }
}
