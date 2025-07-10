using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Netcode;
using UnityEngine.Animations;
using TMPro;

public class Rebel : NetworkBehaviour
{
    #region PROPS AND FIELDS
    [Header("References - Self")]
    [SerializeField] Transform HorizontalRotator;
    Rigidbody rigidBody;
    [SerializeField] public Transform VerticalRotator;
    [SerializeField] public Transform WeaponSpace;
    [SerializeField] public Transform[] weaponSlots;

    [SerializeField] Transform LLeg;
    [SerializeField] Transform RLeg;

    [SerializeField] Transform GroundChecker;
    float groundCheckRayRadius = 0.3f;
    float groundCheckRayRange = 0.2f;

    [SerializeField] public Transform RecoilPointer;

    [SerializeField] public TextMeshProUGUI RoundTimerUI;


    [Header("References - External")]
    [SerializeField] WeaponLookupSO weaponLookup;
    [SerializeField] public NetworkObject nade;  // TEMP
    [SerializeField] public NetworkObject Package;  // for testing
    [SerializeField] public NetworkObject KTLaunchPad;  // for testing


    [Header("Movement - Basic")]
    [SerializeField] float jumpForce = 8.5f; // force of the jump
    float acceleration = 55; // force multiplier to acceleration force
    float deceleration = 25; // force multiplier to deceleration force
    int defaultMaxVelocity = 10; // used in case no weapon is equipped (when speed exceeds this value, set movespeed to this value instead.)
    float jumpCooldown = 0.1f; // minimum time between jumping
    float timeOfLastJump = 0f;  // used to check if jump is on cooldown
    [SerializeField] float jumpInputBuffer = 0.1f; // allows you to input jump before jump is available by n seconds
    float timeOfJumpLastInputted = 0f; // used to check if jump input is being buffered
    [SerializeField] float downwardGravity = 1f;  // how much extra gravity is experienced while velocity y component is NEGATIVE
    [SerializeField] float upwardGravity = 1f;  // how much extra gravity is experienced while velocity y component is POSITIVE
    [SerializeField] public Vector3 Move { get; private set; }
    bool inAir = true;

    WeaponSlot equippedWeaponSlot;

    Dictionary<WeaponSlot, GameObject> weapons = new Dictionary<WeaponSlot, GameObject>();

    // detecting nearby weapons
    float pickupRange = 5f;  // how close a rebel needs to be to pick up weapon

    [Header("Movement - Air Control")]
    [SerializeField] public float slerpStrength = 0.2f;   // used when player is turning a lot and wants maximum turn speed
    [SerializeField] public float notForward = 0f;
    private float lowVelocityAirControlThreshold = 4;
    [SerializeField] public float airResistanceMult = 0.15f;

    [Header("KT Spells")]
    [SerializeField] public float kTDashDuration = 0.15f;
    private bool kTDashing = false;
    private float currentKTDashDuration = 0f;
    [SerializeField] public float kTDashVelocity = 40;
    private Vector3 currentKTDashDir;
    private float kTDashCD = 1f;
    private float currentKTDashCD = 0f;
    [SerializeField] public float kTDashExitV = 3;
    private float skt8JumpCancelScalar = 0.6f;

    private bool kTSkating = false;
    [SerializeField] public float kTSkateDuration = 4f;
    [SerializeField] public float currentKTSkateDuration;
    private float kTSkateCD = 5f;
    private float currentKTSkateCD = 0f;
    [SerializeField] public GameObject sKT8Indicator;

    [SerializeField] public float throwStrength;
    #endregion PROPS AND FIELDS


    #region CORE METHODS
    void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        ClientSideMgr.Instance.SetClientOwnedRebel(GetComponent<NetworkObject>());

        // if matchMgr does not yet have the local rebel
        if (!MatchMgr.Instance.RecievedLocalRebel)
            MatchMgr.Instance.RegisterRebel(this);

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
        //if (Input.GetKey(KeyCode.Space) || Input.GetAxis("Mouse ScrollWheel") > 0)
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetAxis("Mouse ScrollWheel") > 0)
        {
            bool success = TryJump();

            if (!success) timeOfJumpLastInputted = Time.time;  // jump not possible, so buffer jump input
        }
        if (Time.time < timeOfJumpLastInputted + jumpInputBuffer)  // if jump buffered
        {
            bool success = TryJump();

            if (success) timeOfJumpLastInputted -= 1;  // this stops jump buffer until you next input jump
        }

        // equip weapons
        if (Input.GetKeyDown(KeyCode.Alpha1))
            EquipWeaponRpc(WeaponSlot.Primary);
        if (Input.GetKeyDown(KeyCode.Alpha2))
            EquipWeaponRpc(WeaponSlot.Secondary);
        if (Input.GetKeyDown(KeyCode.Alpha3))
            EquipWeaponRpc(WeaponSlot.Melee);
        if (Input.GetKeyDown(KeyCode.Alpha4))
            EquipWeaponRpc(WeaponSlot.Package);

        // drop weapon
        if (Input.GetKeyDown(KeyCode.G))
            DropWeaponRpc(equippedWeaponSlot);

        // throw nade
        if (Input.GetKeyDown(KeyCode.C))
            ThrowNadeRpc();

        // pickup weapon
        if (Input.GetKeyDown(KeyCode.F))
        {
            // raycast to try hit a weapon
            if (Physics.SphereCast(VerticalRotator.position, 1f, VerticalRotator.forward, out RaycastHit hit, pickupRange, Utils.LayerToLayerMask(Layers.Weapon)))
            {
                // if the weapon has a gameobject and WeaponBase
                if (hit.collider.gameObject != null && hit.collider.gameObject.GetComponent<WeaponBase>() != null)
                {
                    // if the weapon is not the planted package
                    if (!(hit.collider.gameObject.TryGetComponent<Package>(out Package package) && package.Planted))
                    {
                        PickupWeaponRpc(
                            hit.collider.gameObject.GetComponent<WeaponBase>().weaponID,
                            hit.collider.gameObject.GetComponent<WeaponBase>().WeaponSlot,
                            hit.collider.GetComponent<NetworkObject>());
                    }
                }
            }
        }

        // big burst of movement for debugging/testing
        if (Input.GetKeyDown(KeyCode.LeftShift))
            rigidBody.AddForce(Move.normalized * 25f, ForceMode.Impulse);

        // KTDash
        if (Input.GetKey(KeyCode.E)
            && currentKTDashCD <= 0
            && (Mathf.Abs(Input.GetAxisRaw("Vertical")) + Mathf.Abs(Input.GetAxisRaw("Horizontal"))) != 0)
        {
            StartKTDash();
        }

        // SKT8
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (!kTSkating)
                StartKTSkate();
            else
                CancelKTSkate();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            ThrowKTJumpPadRpc();
        }

        // crouching
        if (Input.GetKeyDown(KeyCode.LeftControl))
            ToggleCrouchRpc(true);
        if (Input.GetKeyUp(KeyCode.LeftControl))
            ToggleCrouchRpc(false);

        // spawn Package (testing
        if (Input.GetKeyDown(KeyCode.CapsLock)) SpawnPackageRpc();
    }

    bool TryJump()
    {
        //if jump not on cooldown
        if (Time.time > timeOfLastJump + jumpCooldown)
        {
            if (!inAir && !kTDashing)
            {
                Jump();
                return true;
            }
            if (kTDashing)
            {
                CancelKTDash(false);
                rigidBody.velocity *= skt8JumpCancelScalar;
                Jump();
                return true;
            }
        }

        return false;
    }

    void Jump()
    {
        rigidBody.velocity = Utils.RemoveY(rigidBody.velocity);
        rigidBody.AddForce(jumpForce * rigidBody.mass * Vector3.up, ForceMode.Impulse);
        timeOfLastJump = Time.time;
    }

    [Rpc(SendTo.Server)]
    private void SpawnPackageRpc()
    {
        NetworkObject p = NetworkManager.SpawnManager.InstantiateAndSpawn(Package);
        p.transform.position = Vector3.up * 3f;
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
    #endregion CORE METHODS


    #region CORE MOVEMENT
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
        return dot <= notForward;
        //return dot <= 0.1f;
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

        // make rebel collider shorter
        GetComponent<CapsuleCollider>().height = 3.1f + colliderN;
        GroundChecker.transform.position = GroundChecker.transform.position - Vector3.up * colliderN;
        if (!inAir)
        {
            transform.position = transform.position + Vector3.up * colliderN;
            //GroundChecker.transform.position = Vector3.up * 2f;
        }
    }
    #endregion CORE MOVEMENT


    #region WEAPON
    [Rpc(SendTo.Everyone)]
    public void EquipWeaponRpc(WeaponSlot slot)
    {
        // if no weapon in that slot: do nothing
        if (!weapons.TryGetValue(slot, out GameObject fuckoff))
            return;

        if (weapons.TryGetValue(equippedWeaponSlot, out GameObject weapon) && weapons[equippedWeaponSlot].GetComponent<RecoilMgr>() != null)
            weapon.GetComponent<RecoilMgr>().ResetInaccuracyToZero();

        // unequip current weapon
        if (weapons.TryGetValue(equippedWeaponSlot, out fuckoff))
            weapons[equippedWeaponSlot].SetActive(false);

        // equip new weapon
        equippedWeaponSlot = slot;
        weapons[equippedWeaponSlot].SetActive(true);
        weapons[equippedWeaponSlot].transform.position = Camera.main.transform.position;
        WeaponSpace.localRotation = Quaternion.identity;

        if (IsOwner && weapons[equippedWeaponSlot].GetComponent<TRifle>() != null)
            GetComponent<AmmoDisplayMgr>().gun = weapons[equippedWeaponSlot].GetComponent<TRifle>();

        print($"{{ERPC}} OCID: {OwnerClientId} => equipping {slot}");
    }


    [Rpc(SendTo.Server)]
    public void PickupWeaponRpc(Weapons weapon, WeaponSlot slot, NetworkObjectReference weaponToDespawn)
    {
        print($"{{SRPC}} OCID: {OwnerClientId} => despawning {weapon}");

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


        print($"{{LOCAL}} OCID: {OwnerClientId} => picking up {weapon}");

        WeaponBase wb = NetworkManager.SpawnManager.InstantiateAndSpawn(weaponLookup.Dict[weapon], OwnerClientId).GetComponent<WeaponBase>();
        wb.AttachedRebelNetObjRef.Value = new NetworkObjectReference(NetworkObject);
    }

    /// <summary>
    /// This is called by a weapon when it recieves it's AttachedWeaponNetObjID.
    /// </summary>
    /// <param name="weapon"></param>
    /// <param name="weaponID"></param>
    /// <param name="slot"></param>
    public void Attach(GameObject weapon, Weapons weaponID, WeaponSlot slot)
    {

            print($"{{LOCAL}} OCID:  {OwnerClientId} => attaching {weapon}");
            // drop weapon in the desired slot
            DropWeapon(slot);

            // attach new weapon
            weapons[slot] = weapon;
            weapons[slot].GetComponent<WeaponBase>().SetIsDetachedIfOwner(false);
            weapons[slot].SetActive(false);

        if (IsOwner)
        {
            ParentConstraint pc = weapons[slot].GetComponent<ParentConstraint>();
            
            List<ConstraintSource> constraints = new List<ConstraintSource>() { new ConstraintSource { sourceTransform = weaponSlots[(int)slot], weight = 1 } };
            pc.SetSources(constraints);
            pc.constraintActive = true;
        }
    }

    [Rpc(SendTo.Everyone)]
    public void DropWeaponRpc(WeaponSlot slot)
    {
        DropWeapon(slot);
    }

    [Rpc(SendTo.Everyone)]
    public void DropHighestWeaponRpc()
    {
        bool success = false;
        int weaponSlot = 0;
        while (!success && weaponSlot < 3)
        {
            if (weapons.TryGetValue((WeaponSlot)weaponSlot, out GameObject weapon))
            {
                EquipWeaponRpc((WeaponSlot)weaponSlot);
                DropWeaponRpc((WeaponSlot)weaponSlot);
                success = true;
            }
            weaponSlot++;
        }
    }

    private void DropWeapon(WeaponSlot slot)
    {
        // if no weapon in slot: do nothing
        if (!weapons.TryGetValue(slot, out GameObject w) || !w.activeSelf)
            return;

        print($"{{LOCAL}} OCID:  {OwnerClientId} => dropping {slot}");

        GameObject droppedWeapon = weapons[slot];
        weapons.Remove(slot);

        droppedWeapon.GetComponent<WeaponBase>().SetIsDetachedIfOwner(true);
        droppedWeapon.transform.Rotate(-1 * droppedWeapon.transform.localRotation.eulerAngles.x, 0, 0);
        droppedWeapon.GetComponent<ParentConstraint>().constraintActive = false;
    }
    #endregion WEAPON


    #region ABILITIES
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

    [Rpc(SendTo.Server)]
    private void ThrowKTJumpPadRpc()
    {
        NetworkObject no = NetworkManager.SpawnManager.InstantiateAndSpawn(KTLaunchPad);
        no.transform.position = WeaponSpace.position;
        no.GetComponent<Rigidbody>().velocity = rigidBody.velocity + WeaponSpace.forward * throwStrength;
    }
    #endregion ABILITIES
}
