using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
using UnityEngine.UIElements;
using Unity.Netcode;
using Unity.VisualScripting;
using JetBrains.Annotations;
using UnityEngine.Animations;

public class Tanc : NetworkBehaviour
{
    // self references
    Rigidbody rigidBody;
    [SerializeField] Transform HorizontalRotator;
    [SerializeField] public Transform VerticalRotator;
    [SerializeField] public Transform WeaponSpace;
    [SerializeField] public Transform WeaponSlot1;


    [SerializeField] Transform GroundChecker;
    float groundCheckRayRadius = 0.3f;
    float groundCheckRayRange = 0.2f;

    // states
    float acceleration = 55; // force multiplier to acceleration force
    float deceleration = 25; // force multiplier to deceleration force
    int defaultMaxVelocity = 10; // used in case no weapon is equipped (when speed exceeds this value, set movespeed to this value instead.)
    [SerializeField] float jumpForce = 7.5f; // force of the jump
    [SerializeField] float downwardGravity = 1f;
    [SerializeField] float upwardGravity = 0.7f;
    public Vector3 Move { get; private set; }
    bool inAir = true;

    WeaponSlot equippedWeaponSlot;

    Dictionary<WeaponSlot, GameObject> weapons = new Dictionary<WeaponSlot, GameObject>();

    // detecting nearby weapons
    float pickupRange = 5f;  // how close a tanc needs to be to pick up weapon
    float weaponPickupAngle = 1f - (50f / 90f);  // the angle within which you can pick up a weapon (i.e. how accurate you need to aim at it)

    [SerializeField] WeaponLookupSO weaponLookup;

    //TEMP
    public NetworkObject nade;

    public float slerpStrength;
    private float lowVelocityAirControlThreshold = 4;
    public float airResistanceMult = 1.5f;

    private bool kTDashing = false;
    public float kTDashDuration = 0.5f;
    private float currentKTDashDuration = 0f;
    public float kTDashVelocity = 15;
    private Vector3 currentKTDashDir;
    private float kTDashCD = 1f;
    private float currentKTDashCD = 0f;
    public float kTDashExitV = 3;

    private bool kTSkating = false;
    public float kTSkateDuration = 4f;
    public float currentKTSkateDuration;
    private float kTSkateCD = 5f;
    private float currentKTSkateCD = 0f;
    public GameObject sKT8Indicator;

    private float skt8JumpCancelScalar = 0.6f;

    [SerializeField] Transform LLeg;
    [SerializeField] Transform RLeg;


    public GameObject RecoilPointer;

    void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        ClientSideMgr.Instance.SetClientOwnedTanc(GetComponent<NetworkObject>());
        sKT8Indicator = GameObject.FindGameObjectsWithTag("sKT8Indicator")[0];  // temporary mode of finding gameobject - should be improved
        sKT8Indicator.SetActive(false);
    }


    void Update()
    {
        if (!IsOwner) return;

        if (currentKTDashCD > 0)
            currentKTDashCD -= Time.deltaTime;
        if (currentKTSkateCD > 0)
            currentKTSkateCD -= Time.deltaTime;

        // jump
        if ((Input.GetKeyDown(KeyCode.Space) || Input.GetAxis("Mouse ScrollWheel") > 0) && !inAir)
        {
            rigidBody.AddForce(jumpForce * rigidBody.mass * Vector3.up, ForceMode.Impulse);
            if (kTDashing)
            {
                CancelKTDash(false);
                rigidBody.velocity = rigidBody.velocity * skt8JumpCancelScalar;
            }
        }

        // equip weapons
        if (Input.GetKeyDown(KeyCode.Alpha1))
            EquipWeaponRpc(WeaponSlot.Primary);
        if (Input.GetKeyDown(KeyCode.Alpha2))
            EquipWeaponRpc(WeaponSlot.Secondary);
        if (Input.GetKeyDown(KeyCode.Alpha3))
            EquipWeaponRpc(WeaponSlot.Melee);

        // drop weapon
        if (Input.GetKeyDown(KeyCode.G))
            DropWeaponRpc(equippedWeaponSlot);

        // throw nade
        if (Input.GetKeyDown(KeyCode.C))
            ThrowNadeRpc();

        // pickup weapon
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (Physics.SphereCast(VerticalRotator.position, 1f, VerticalRotator.forward, out RaycastHit hit, pickupRange, Utils.LayerToLayerMask(Layers.Weapon)))
            {
                if (hit.collider.gameObject != null && hit.collider.gameObject.GetComponent<WeaponBase>() != null)
                {
                    PickupWeaponRpc(
                        hit.collider.gameObject.GetComponent<WeaponBase>().weaponID,
                        hit.collider.gameObject.GetComponent<WeaponBase>().WeaponSlot,
                        hit.collider.GetComponent<NetworkObject>());
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftShift))
            rigidBody.AddForce(Move.normalized * 25f, ForceMode.Impulse);

        if (Input.GetKey(KeyCode.E)
            && currentKTDashCD <= 0
            && (Mathf.Abs(Input.GetAxisRaw("Vertical")) + Mathf.Abs(Input.GetAxisRaw("Horizontal"))) != 0)
        {
            StartKTDash();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (!kTSkating)
                StartKTSkate();
            else
                CancelKTSkate();
        }

        if (Input.GetKeyDown(KeyCode.LeftControl))
            ToggleCrouchRpc(true);
        if (Input.GetKeyUp(KeyCode.LeftControl))
            ToggleCrouchRpc(false);
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        // get directional inputs
        float xMov = Input.GetAxisRaw("Horizontal");
        float zMov = Input.GetAxisRaw("Vertical");

        // get inputted movement relative to the rotator/camera rotation
        Move = ((HorizontalRotator.forward) * zMov + HorizontalRotator.right * xMov);

        CalculateMovement();


        // Add downward force in addition to existing gravity when jumping (more when falling).
        float x = (rigidBody.velocity.y <= 0) ? downwardGravity : upwardGravity;
        //float x = (rigidBody.velocity.y <= 0) ? 1 : 0.7f;
        rigidBody.AddForce(Physics.gravity * x, ForceMode.Acceleration);

        GroundCheck(groundCheckRayRadius, groundCheckRayRange);

        if (kTDashing) KTDash();
        if (kTSkating) KTSkate();
    }

    void GroundCheck(float rayRadius, float rayRange)
    {
        // start spherecast from slight above groundchecker (if origin is within radius of target, then sphere cast shits itself and dies and does not work)
        Vector3 origin = GroundChecker.position + Vector3.up * (groundCheckRayRadius + 0.05f);
        inAir = !Physics.SphereCast(origin, rayRadius, Vector3.down, out RaycastHit hit, rayRange, Utils.LayerToLayerMask(Layers.SolidGround));
    }

    /// <summary>
    /// if movement input not forward (within custom threshold)
    /// </summary>
    /// <param name="dot"></param>
    /// <returns></returns>
    bool DotNotForward(float dot)
    {
        return dot <= 0.1f;
    }


    /// <summary>
    /// if movement input not directly backwards (within custom threshold)
    /// </summary>
    /// <param name="dot"></param>
    /// <returns></returns>
    bool DotNotDirectlyBackwards(float dot)
    {
        return dot >= -0.8f;
    }

    // redistribute velocity
    void CalculateMovement()
    {
        if (kTDashing) return;

        // get velocity without y component
        Vector3 xzVelocity = new Vector3(rigidBody.velocity.x, 0, rigidBody.velocity.z);

        // get max velocity
        float maxV = weapons.ContainsKey(equippedWeaponSlot)
            ? weapons[equippedWeaponSlot].GetComponent<WeaponBase>().MaxVelocity
            : defaultMaxVelocity;


        ///////////////////////////////////// REDUCE SPEED IF OVER MAXIMUM VELOCITY ///////////////////////////
        // if the user is trying to move and current velocity > maximum velocity:
        // prevent them from speeding up, but allow them to direct and counteract their currently high velocity
        // This is only run whilst on the ground
        if (Move != Vector3.zero && xzVelocity.magnitude > maxV)
        //if (!inAir && !kTSkating && Move != Vector3.zero && xzVelocity.magnitude > maxV)
        {
            float strength = (inAir || kTSkating) ? airResistanceMult : 1f;
            float A = Vector3.SignedAngle(xzVelocity * -1, Move, new Vector3(1, 0, 1));
            float aRadian = Mathf.Abs(A / 180);

            // reduce velocity in current direction according to how much it counteracts the current velocity, AND according to how far over maxV you are moving
            rigidBody.AddForce(
                xzVelocity.normalized
                * (-1 * Move.magnitude * acceleration * aRadian)
                * (1 + (xzVelocity.magnitude - maxV) * Time.fixedDeltaTime)
                * strength);
        }


        ///////////////////////////////////// APPLY NEW MOVEMENT ///////////////////////////
        if (inAir || kTSkating)  // AIR CONTROL
        {
            // use dot product to determine how aligned or misaligned current velocity and movement input are
            float dot = Vector3.Dot(xzVelocity.normalized, Move.normalized); 

            if (Move != Vector3.zero)
            {
                // if movement inputted is not aligned with current momentum
                if (DotNotForward(dot))  
                {
                    // AIR STRAFE
                    // if movement inputted is nearly perpendicular to current momentum (within custom threshold):
                    //       use slerp toredirect velocity with a certain level effectiveness (which scales linearly with the inverse of the dot product)
                    if (DotNotDirectlyBackwards(dot))
                        rigidBody.velocity = Vector3.Slerp(xzVelocity.normalized, Move.normalized, slerpStrength * (1 - Mathf.Abs(dot)))
                            * xzVelocity.magnitude
                            + new Vector3(0, rigidBody.velocity.y, 0);

                    // JUMP PEEK
                    // if movement inputted is directly opposite to current momentum (within custom threshold):
                    //      redirect momentum using lerp
                    else
                        rigidBody.velocity = Vector3.Lerp(xzVelocity, Move.normalized, 0.1f)
                            + new Vector3(0, rigidBody.velocity.y, 0);
                }

                // if velocity is below a custom threshold:
                //      allow some amount of movement
                if (xzVelocity.magnitude < lowVelocityAirControlThreshold)
                    rigidBody.AddForce(Move * acceleration);
            }
        }
        else  // GROUND MOVEMENT
        {
            rigidBody.AddForce(Move * acceleration);
        }


        ///////////////////////////////////// DECELLERATION (if not inputting movement) ///////////////////////////
        if (Move.magnitude < 0.1f && !inAir && !kTSkating)
        {
            if (xzVelocity.magnitude < 0.1f)
            {
                rigidBody.velocity = new Vector3(0, rigidBody.velocity.y, 0);
            }
            else
            {
                rigidBody.AddForce(new Vector3(-rigidBody.velocity.x, 0, -rigidBody.velocity.z).normalized * deceleration);
            }
        }
        //if (Move.magnitude < 0.1f || inAir || kTSkating)
        ////if (Move.magnitude < 0.1f && !inAir && !kTSkating)
        //{
        //    float strength = (inAir || kTSkating) ? airResistanceMult : 1f;
        //    if (xzVelocity.magnitude < 0.1f)
        //    {
        //        rigidBody.velocity = new Vector3(0, rigidBody.velocity.y, 0);
        //    }
        //    else
        //    {
        //        rigidBody.AddForce(
        //            new Vector3(-rigidBody.velocity.x, 0, -rigidBody.velocity.z).normalized
        //            * deceleration
        //            * strength);
        //    }
        //}
    }

    [Rpc(SendTo.Everyone)]
    public void EquipWeaponRpc(WeaponSlot slot)
    {
        // if no weapon in that slot: do nothing
        if (!weapons.TryGetValue(slot, out GameObject fuckoff))
            return;

        // unequip current weapon
        if (weapons.TryGetValue(equippedWeaponSlot, out fuckoff))
            weapons[equippedWeaponSlot].SetActive(false);

        // equip new weapon
        equippedWeaponSlot = slot;
        weapons[equippedWeaponSlot].SetActive(true);

        if (IsOwner && weapons[equippedWeaponSlot].GetComponent<TRifle>() != null)
            GetComponent<AmmoDisplayMgr>().gun = weapons[equippedWeaponSlot].GetComponent<TRifle>();

        print($"{{ERPC}} NOID: {NetworkObjectId} => equipping {slot}");
    }


    [Rpc(SendTo.Server)]
    public void PickupWeaponRpc(Weapons weapon, WeaponSlot slot, NetworkObjectReference weaponToDespawn)
    {
        print($"{{SRPC}} NOID: {NetworkObjectId} => despawning {weapon}");

        // despawn old weapon
        weaponToDespawn.TryGet(out NetworkObject _weaponToDespawn);
        _weaponToDespawn.Despawn();

        PickupWeapon(weapon, slot);
    }

    [Rpc(SendTo.Server)]
    public void PickupWeaponRpc(Weapons weapon, WeaponSlot slot)
    {
        PickupWeapon(weapon, slot);
    }

    private void PickupWeapon(Weapons weapon, WeaponSlot slot)
    {
        if (!IsServer)
            throw new Exception($"{nameof(PickupWeapon)}() method invoked from NotServer. Should only be called by Server RPC '{nameof(PickupWeaponRpc)}");


        print($"{{SRPC}} NOID: {NetworkObjectId} => picking up {weapon}");

        WeaponBase wb = NetworkManager.SpawnManager.InstantiateAndSpawn(weaponLookup.Dict[weapon], OwnerClientId).GetComponent<WeaponBase>();
        wb.AttachedTancNetObjRef.Value = new NetworkObjectReference(NetworkObject);
    }

    /// <summary>
    /// This is called by a weapon when it recieves it's AttachedWeaponNetObjID.
    /// </summary>
    /// <param name="weapon"></param>
    /// <param name="weaponID"></param>
    /// <param name="slot"></param>
    public void Attach(GameObject weapon, Weapons weaponID, WeaponSlot slot)
    {

            print($"{{LOCAL}} NOID: {NetworkObjectId} => attaching {weapon}");
            // drop weapon in the desired slot
            DropWeapon(slot);

            // attach new weapon
            weapons[slot] = weapon;
            weapons[slot].GetComponent<WeaponBase>().SetIsDetachedIfOwner(false);
            weapons[slot].SetActive(false);

        if (IsOwner)
        {
            ParentConstraint pc = weapons[slot].GetComponent<ParentConstraint>();
            List<ConstraintSource> constraints = new List<ConstraintSource>() { new ConstraintSource { sourceTransform = WeaponSlot1, weight = 1 } };
            pc.SetSources(constraints);
            pc.constraintActive = true;
        }
    }

    [Rpc(SendTo.Everyone)]
    public void DropWeaponRpc(WeaponSlot slot)
    {
        DropWeapon(slot);
    }

    public void DropWeapon(WeaponSlot slot)
    {
        // if no weapon in slot: do nothing
        if (!weapons.TryGetValue(slot, out GameObject fuckoff))
            return;

        print($"{{ERPC}} NOID: {NetworkObjectId} => dropping {slot}");

        GameObject droppedWeapon = weapons[slot];
        weapons.Remove(slot);

        droppedWeapon.GetComponent<WeaponBase>().SetIsDetachedIfOwner(true);
        droppedWeapon.transform.Rotate(-1 * droppedWeapon.transform.localRotation.eulerAngles.x, 0, 0);
        droppedWeapon.GetComponent<ParentConstraint>().constraintActive = false;
    }

    [Rpc(SendTo.Server)]
    private void ToggleCrouchRpc(bool crouch)
    {
        float legN = (crouch) ? 0.5f : 0;
        float colliderN = (crouch) ? -0.5f : 0.5f;

        // make legs smaller
        LLeg.localScale = new Vector3(1, 1 - legN, 1);
        RLeg.localScale = new Vector3(1, 1 - legN, 1);

        // move legs upwards
        LLeg.localPosition = new Vector3(0, legN, 0);
        RLeg.localPosition = new Vector3(0, legN, 0);

        // make tanc collider shorter
        GetComponent<CapsuleCollider>().height = 3.1f + colliderN;
        GroundChecker.transform.position = GroundChecker.transform.position - Vector3.up * colliderN;
        if (!inAir)
        {
            transform.position = transform.position + Vector3.up * colliderN;
            //GroundChecker.transform.position = Vector3.up * 2f;
        }
    }


    [Rpc(SendTo.Server)]
    private void ThrowNadeRpc()
    {
        NetworkObject nadeTemp = NetworkManager.SpawnManager.InstantiateAndSpawn(nade, OwnerClientId);

        nadeTemp.GetComponent<NadeScript>().InitializeTransformRpc(VerticalRotator.transform.position + VerticalRotator.transform.forward * 2, VerticalRotator.transform.rotation);
    }

    private void StartKTDash()
    {
        kTDashing = true;
        currentKTDashDuration = kTDashDuration;
        currentKTDashCD = kTDashCD;
        currentKTDashDir = ((HorizontalRotator.forward) * Input.GetAxisRaw("Vertical") + HorizontalRotator.right * Input.GetAxisRaw("Horizontal"));
    }

    private void KTDash()
    {
        if (currentKTDashDuration <= 0)
        {
            CancelKTDash(true);
            return;
        }

        currentKTDashDuration -= Time.fixedDeltaTime;

        rigidBody.velocity = currentKTDashDir.normalized * kTDashVelocity;
    }

    private void CancelKTDash(bool stopMomentum)
    {
        kTDashing = false;
        if (stopMomentum)
            rigidBody.velocity = kTDashExitV * Move.normalized;
        else
            rigidBody.velocity *= 1;

        currentKTDashDuration = 0;
    }

    private void StartKTSkate()
    {
        if (kTSkating)
        {
            CancelKTSkate();
            return;
        }

        currentKTSkateCD = kTSkateCD;
        currentKTSkateDuration = kTSkateDuration;
        kTSkating = true;
        sKT8Indicator.SetActive(true);

        if (kTDashing)
            CancelKTDash(false);
    }

    private void KTSkate()
    {
        if (currentKTSkateDuration <= 0)
        {
            CancelKTSkate();
            return;
        }

        currentKTSkateDuration -= Time.fixedDeltaTime;
    }

    private void CancelKTSkate()
    {
        kTSkating = false;
        currentKTSkateDuration = 0f;
        sKT8Indicator.SetActive(false);
    }
}
