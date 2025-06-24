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

    float inaccuracyScalar = 0;
    GameObject recoilPointer;
    readonly float maxVerticalRecoil = -10;
    readonly float maxHorizontalDeviation = 2f;
    readonly float recoilDecayRate = 0.5f;
    float movePenalty = 0.5f;
    [SerializeField] float BulletSpeed = 200f;
    [SerializeField] GameObject BulletTrail;
    public AmmoMgr ammoMgr;

    private void Start()
    {
        ammoMgr.ResetAmmo();
    }

    protected override void OnUpdate()
    {
        if (!IsOwner)
            return;

        if (Time.time < ammoMgr.GetReloadEndTime() - 0.3f && !ammoMgr.GetReloadDone())
        {
            //print((Time.time - reloadStartTime) / reloadTime);

            Transform sourceTransform = GetComponent<ParentConstraint>().GetSource(0).sourceTransform;

            sourceTransform.Rotate(Mathf.Lerp(0, 720, Time.deltaTime / (ammoMgr.GetReloadTime() - 0.3f)), 0, 0);
        }

        if (Input.GetKeyDown(KeyCode.R))
            ReloadStart();

        if (AttachedTanc != null)
        {
            fireDelay -= Time.deltaTime;

            if (Input.GetKey(KeyCode.Mouse0))
                ShootCheck();

            if (inaccuracyScalar > 0)  // update weaponspace to match the recoil angle
                AttachedTanc.WeaponSpace.localRotation = Quaternion.Euler(
                    recoilPointer.transform.localRotation.eulerAngles.x,
                    recoilPointer.transform.localRotation.eulerAngles.y,
                    0);
        }

        // recover from recoil over time (decay)
        // if you drop the weapon while it has recoil, we still want the recoil to go down to 0 before someone picks it up
        if (inaccuracyScalar > 0 && (!Input.GetKey(KeyCode.Mouse0) || ammoMgr.GetReloadEndTime() > Time.time))
        {
            float multiplier = (inaccuracyScalar > 0.5f) ? 2f : 1f;
            inaccuracyScalar -= recoilDecayRate * Time.deltaTime * multiplier;  // this maybe should not be done in update / with Time.deltaTime

            // handles cases where the rotation is negative
            // e.g. a rotation of -2 will actually be stored as 358. hence, we need to force it to be -2.
            float currHor = recoilPointer.transform.localRotation.eulerAngles.y;
            if (currHor > 180) currHor -= 360;

            // reduce recoilPointer rotation
            recoilPointer.transform.localRotation = Quaternion.Euler(
                maxVerticalRecoil * inaccuracyScalar,
                currHor * inaccuracyScalar,
                0);
        }

        // Check For Reload Finish
        if (!ammoMgr.GetReloadStarted() && !ammoMgr.GetReloadDone())
        {
            GetComponent<ParentConstraint>().GetSource(0).sourceTransform.localRotation = Quaternion.identity;
            ammoMgr.ResetAmmo();
        }
    }

    private void ReloadStart()
    {
        ammoMgr.ReloadStartNow();
    }

    // Override this on semi auto weapon, redundant on full auto weapons
    protected virtual void ShootCheck()
    {
        Shoot();
    }

    public override void Shoot()
    {
        if (fireDelay > 0) return;

        if (ammoMgr.IsOutOfAmmo() || ammoMgr.GetReloadStarted()) return;

        fireDelay = fireDelayMax;
        ammoMgr.ReduceAmmoOnce();

        if (ammoMgr.IsOutOfAmmo())
            ReloadStart();

        SpawnShootSoundFxRpc();

        // calculate base inaccuracy
        float currentHorizontal = recoilPointer.transform.localRotation.eulerAngles.y;
        float potentialDeviation = maxHorizontalDeviation * inaccuracyScalar;
        float newHor = currentHorizontal + potentialDeviation * Random.Range(-1f, 1f);

        recoilPointer.transform.localRotation = Quaternion.Euler(maxVerticalRecoil * inaccuracyScalar, newHor, 0);

        // add movement penalty
        float currentSpeed = AttachedTanc.GetComponent<Rigidbody>().velocity.magnitude;
        float xPenalty = movePenalty * currentSpeed * Random.Range(-1f, 1f);
        float yPenalty = movePenalty * currentSpeed * Random.Range(-1f, 1f);

        recoilPointer.transform.localRotation = Quaternion.Euler(
            recoilPointer.transform.localEulerAngles.x + xPenalty,
            recoilPointer.transform.localEulerAngles.y + yPenalty,
            recoilPointer.transform.localEulerAngles.z);

        // perform raycast
        if (AttachedTanc != null && Physics.Raycast(AttachedTanc.VerticalRotator.position, recoilPointer.transform.forward, out RaycastHit hit, maxDistance))
        {
            if (hit.collider.gameObject.layer == (int)Layers.Tanc)
                hit.collider.GetComponent<HitboxScript>().DealDamage(baseDamage);

            //else if (hit.collider.gameObject.layer == (int)Layers.SolidGround || hit.collider.gameObject.layer == (int)Layers.Default)
            SpawnBulletVisualsRpc(hit.point, true);

        }
        // do animation even if it doesn't hit anything
        else
        {
            SpawnBulletVisualsRpc(recoilPointer.transform.position + recoilPointer.transform.forward * 100, false);
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


    protected override void OnAttachedTancNetObjIDChanged(NetworkObjectReference prev, NetworkObjectReference curr)
    {
        base.OnAttachedTancNetObjIDChanged(prev, curr);

        recoilPointer = AttachedTanc.RecoilPointer;
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

    public void ResetInaccuracyToZero()
    {
        inaccuracyScalar = 0f;
    }
}