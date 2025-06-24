using System.Collections;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;

public class TRifle : WeaponBase
{
    public override float MaxVelocity { get; } = 7f;
    protected override int baseDamage { get; set; } = 44;
    protected override float critMultiplier { get; set; } = 3f;
    protected override float limbMultiplier { get; set; } = 0.8f;
    public override WeaponSlot WeaponSlot { get; } = WeaponSlot.Primary;

    private float maxDistance = 150f;

    [SerializeField] Transform tip;
    [SerializeField] private GameObject bulletHolePrefab;


    [SerializeField] float BulletSpeed = 200f;
    [SerializeField] GameObject BulletTrail;
    public AmmoMgr ammoMgr;
    public RecoilMgr recoilMgr;

    private void Start()
    {
        ammoMgr.ResetAmmo();
    }

    protected override void OnUpdate()
    {
        if (!IsOwner)
            return;

        // if reloading: do rotating animation
        if (Time.time < ammoMgr.GetReloadEndTime() - 0.3f && !ammoMgr.GetReloadDone())
        {
            Transform sourceTransform = GetComponent<ParentConstraint>().GetSource(0).sourceTransform;
            sourceTransform.Rotate(Mathf.Lerp(0, 720, Time.deltaTime / (ammoMgr.GetReloadTime() - 0.3f)), 0, 0);
        }

        if (Input.GetKeyDown(KeyCode.R)) ammoMgr.ReloadStartNow();

        if (AttachedTanc != null)
        {
            fireDelay -= Time.deltaTime;

            if (Input.GetKey(KeyCode.Mouse0))
                ShootCheck();

            recoilMgr.UpdateWeaponSpace();
        }

        // recover from recoil over time (decay)
        // if you drop the weapon while it has recoil, we still want the recoil to go down to 0 before someone picks it up
        recoilMgr.DecayRecoil(ammoMgr);

        // Check For Reload Finish
        if (!ammoMgr.ReloadStarted() && !ammoMgr.GetReloadDone())
        {
            GetComponent<ParentConstraint>().GetSource(0).sourceTransform.localRotation = Quaternion.identity;
            ammoMgr.ResetAmmo();
        }
    }


    // Override this on semi auto weapon
    protected virtual void ShootCheck() 
    {
        if (fireDelay > 0) return;
        if (ammoMgr.IsOutOfAmmo() || ammoMgr.ReloadStarted()) return;

        Shoot();
    }

    public override void Shoot()
    {
        fireDelay = fireDelayMax;
        ammoMgr.ReduceAmmoOnce();

        SpawnShootSoundFxRpc();

        recoilMgr.CalculateBaseInaccuracy();

        recoilMgr.AddMovementPenalty();

        // perform raycast
        if (AttachedTanc != null && Physics.Raycast(AttachedTanc.VerticalRotator.position, recoilMgr.recoilPointer.transform.forward, out RaycastHit hit, maxDistance))
        {
            if (hit.collider.gameObject.layer == (int)Layers.Tanc)
                hit.collider.GetComponent<HitboxScript>().DealDamage(baseDamage);

            //else if (hit.collider.gameObject.layer == (int)Layers.SolidGround || hit.collider.gameObject.layer == (int)Layers.Default)
            SpawnBulletVisualsRpc(hit.point, true);

        }
        // do animation even if it doesn't hit anything
        else
        {
            SpawnBulletVisualsRpc(recoilMgr.recoilPointer.transform.position + recoilMgr.recoilPointer.transform.forward * 100, false);
        }

        recoilMgr.IncreaseInaccuracy();

        // Check if I am now out of ammo, and if so: reload
        if (ammoMgr.IsOutOfAmmo())
            ammoMgr.ReloadStartNow();
    }

    [Rpc(SendTo.Everyone)]
    private void SpawnShootSoundFxRpc()
    {
        Instantiate(ShootSfx, transform.position, transform.rotation);
    }


    protected override void OnAttachedTancNetObjIDChanged(NetworkObjectReference prev, NetworkObjectReference curr)
    {
        base.OnAttachedTancNetObjIDChanged(prev, curr);

        recoilMgr.recoilPointer = AttachedTanc.RecoilPointer;
    }



    [Rpc(SendTo.Everyone)]
    private void SpawnBulletVisualsRpc(Vector3 hitPoint, bool spawnHole = false)
    {
        // spawn trail
        TrailRenderer trail = Instantiate(BulletTrail, tip.position, Quaternion.identity).GetComponent<TrailRenderer>();
        StartCoroutine(SpawnTrail(trail, hitPoint));

        // spawn hole
        if (spawnHole) Instantiate(bulletHolePrefab, hitPoint, Quaternion.identity);
    }

    // trail logic adapted from: https://github.com/llamacademy/raycast-bullet-trails/blob/main/Assets/Scripts/Gun.cs
    private IEnumerator SpawnTrail(TrailRenderer Trail, Vector3 HitPoint)
    {
        // This has been updated from the video implementation to fix a commonly raised issue about the bullet trails
        // moving slowly when hitting something close, and not
        Vector3 startPosition = Trail.transform.position;
        float distance = Vector3.Distance(Trail.transform.position, HitPoint);
        float remainingDistance = distance;

        while (remainingDistance > 0)
        {
            Trail.transform.position = Vector3.Lerp(startPosition, HitPoint, 1 - (remainingDistance / distance));

            remainingDistance -= BulletSpeed * Time.deltaTime;

            yield return null;
        }
        Trail.transform.position = HitPoint;

        Destroy(Trail.gameObject, Trail.time);
    }
}