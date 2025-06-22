using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TRifle : WeaponBase
{
    private float fireDelay;

    public override float MaxVelocity { get; } = 7;
    protected override int baseDamage { get; set; } = 44;
    protected override float critMultiplier { get; set; } = 3f;
    protected override float limbMultiplier { get; set; } = 0.8f;
    public override WeaponSlot WeaponSlot { get; } = WeaponSlot.Primary;
    //private float rayRadius = 0.3f;
    private float maxDistance = 150f;


    protected override void OnUpdate()
    {
        if (!IsOwner)
            return;

        fireDelay -= Time.deltaTime;

        if (Input.GetKey(KeyCode.Mouse0))
            Shoot();
    }

    public override void Shoot()
    {
        if (fireDelay > 0) return;

        SpawnShootFxRpc();

        fireDelay = 0.11f;

        if (AttachedTanc != null && Physics.Raycast(AttachedTanc.VerticalRotator.position, AttachedTanc.VerticalRotator.forward, out RaycastHit hit, maxDistance))
        {
            if (hit.collider.gameObject.layer == (int)Layers.Tanc)
                print("Tanc hit!");

            if (hit.collider.GetComponent<HitboxScript>() != null)
                hit.collider.GetComponent<HitboxScript>().DealDamage(baseDamage);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void SpawnShootFxRpc()
    {
        Instantiate(ShootSfx, transform.position, transform.rotation);
    }
}