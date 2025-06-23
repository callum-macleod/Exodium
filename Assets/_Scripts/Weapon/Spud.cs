using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spud : TRifle
{
    public override float MaxVelocity { get; } = 8;
    public override WeaponSlot WeaponSlot { get; } = WeaponSlot.Secondary;
    protected override int baseDamage { get; set; } = 20;
    protected override float critMultiplier { get; set; } = 3f;
    protected override float limbMultiplier { get; set; } = 0.8f;

    protected override void ShootCheck()
    {
        if (!Input.GetKeyDown(KeyCode.Mouse0))
            return;

        base.ShootCheck();
    }
}
