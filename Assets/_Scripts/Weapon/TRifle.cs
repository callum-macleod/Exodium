using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TRifle : WeaponBase
{
    private float fireDelay;

    public override float MaxVelocity { get; } = 7f;
    protected override int baseDamage { get; set; } = 44;
    protected override float critMultiplier { get; set; } = 3f;
    protected override float limbMultiplier { get; set; } = 0.8f;
    public override WeaponSlot WeaponSlot { get; } = WeaponSlot.Primary;
    //private float rayRadius = 0.3f;
    private float maxDistance = 150f;

    [SerializeField] private NetworkObject bulletHolePrefab;

    float inaccuracyScalar = 0;
    GameObject recoilPointer;
    readonly float maxVerticalRecoil = -10;
    readonly float maxHorizontalDeviation = 2f;
    readonly float recoilDecayRate = 0.5f;


    protected override void OnUpdate()
    {
        if (!IsOwner)
            return;

        if (AttachedTanc != null)
        {
            fireDelay -= Time.deltaTime;

            if (Input.GetKey(KeyCode.Mouse0))
                Shoot();

            if (inaccuracyScalar != 0 || transform.localRotation.x != 0)  // if both values are 0: do nothing
                AttachedTanc.WeaponSpace.localRotation = Quaternion.Euler(maxVerticalRecoil * inaccuracyScalar, 0, 0);
        }

        // if you drop the weapon while it has recoil, we still want the recoil to go down to 0 before someone picks it up
        if (inaccuracyScalar > 0 && !Input.GetKey(KeyCode.Mouse0))
        {
            inaccuracyScalar -= recoilDecayRate * Time.deltaTime;  // this maybe should not be done in update / with Time.deltaTime
            float currHor = recoilPointer.transform.localRotation.eulerAngles.y;
            recoilPointer.transform.localRotation = Quaternion.Euler(
                recoilPointer.transform.localRotation.eulerAngles.x,
                currHor * inaccuracyScalar,
                recoilPointer.transform.localRotation.eulerAngles.z);
        }
    }

    public override void Shoot()
    {
        if (fireDelay > 0) return;

        fireDelay = 0.11f;
        SpawnShootSoundFxRpc();

        float currentHorizontal = recoilPointer.transform.localRotation.eulerAngles.y;
        float potentialDeviation = maxHorizontalDeviation * inaccuracyScalar;
        float newHor = currentHorizontal + potentialDeviation * Random.Range(-1f, 1f);
        print($"{currentHorizontal} + {potentialDeviation * Random.Range(-1f, 1f)} = {newHor}");

        // clamp to some max horizontal recoil
        //if (Mathf.Abs(newHor) > 5f)
        //    newHor = Mathf.Abs(newHor) / newHor * 5f;


        recoilPointer.transform.localRotation = Quaternion.Euler(maxVerticalRecoil * inaccuracyScalar, newHor, 0);

        if (AttachedTanc != null && Physics.Raycast(AttachedTanc.VerticalRotator.position, recoilPointer.transform.forward, out RaycastHit hit, maxDistance))
        {
            if (hit.collider.gameObject.layer == (int)Layers.Tanc)
                print("Tanc hit!");
            else if (hit.collider.gameObject.layer == (int)Layers.SolidGround || hit.collider.gameObject.layer == (int)Layers.Default)
                SpawnBulletHoleRpc(hit.point);
                    

            if (hit.collider.GetComponent<HitboxScript>() != null)
                hit.collider.GetComponent<HitboxScript>().DealDamage(baseDamage);
        }

        // increase inaccuracy (cap at 1)
        inaccuracyScalar += 0.05f;
        if (inaccuracyScalar > 1) inaccuracyScalar = 1f;
    }

    [Rpc(SendTo.Everyone)]
    private void SpawnShootSoundFxRpc()
    {
        Instantiate(ShootSfx, transform.position, transform.rotation);
    }

    [Rpc(SendTo.Server)]
    private void SpawnBulletHoleRpc(Vector3 pos)
    {
        NetworkObject bh = NetworkManager.SpawnManager.InstantiateAndSpawn(bulletHolePrefab);
        bh.transform.position = pos;
    }


    protected override void OnAttachedTancNetObjIDChanged(NetworkObjectReference prev, NetworkObjectReference curr)
    {
        base.OnAttachedTancNetObjIDChanged(prev, curr);

        recoilPointer = AttachedTanc.RecoilPointer;
    }
}