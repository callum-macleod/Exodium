using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TRifle : WeaponBase
{
    protected override int baseDamage { get; set; } = 44;
    protected override float critMultiplier { get; set; } = 3f;
    protected override float limbMultiplier { get; set; } = 0.8f;
    public override WeaponSlot WeaponSlot { get; } = WeaponSlot.Primary;
    //private float rayRadius = 0.3f;
    private float maxDistance = 150f;

    [SerializeField] 


    protected override void OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
            Shoot();
    }

    public override void Shoot()
    {
        print($"{name}: Shooting");

        if (AttachedTanc != null && Physics.Raycast(AttachedTanc.VerticalRotator.position, AttachedTanc.VerticalRotator.forward, out RaycastHit hit, maxDistance))
        {
            if (hit.collider.gameObject.layer == (int)Layers.Tanc)
                print("Tanc hit!");

            if (hit.collider.GetComponent<HealthManager>() != null)
                hit.collider.GetComponent<HealthManager>().ApplyDamageRpc(baseDamage);
        }
    }
}