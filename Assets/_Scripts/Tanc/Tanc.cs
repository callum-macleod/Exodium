using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
using UnityEngine.UIElements;
using Unity.Netcode;
using Unity.VisualScripting;
using JetBrains.Annotations;

public class Tanc : NetworkBehaviour
{
    // self references
    Rigidbody rigidBody;
    [SerializeField] Transform HorizontalRotator;
    [SerializeField] public Transform VerticalRotator;
    [SerializeField] Transform weaponSpace;

    [SerializeField] Transform GroundChecker;
    float groundCheckRayRadius = 0.3f;
    float groundCheckRayRange = 0.2f;

    // states
    float acceleration = 55; // force multiplier to acceleration force
    float deceleration = 25; // force multiplier to deceleration force
    int defaultMaxVelocity = 10; // used in case no weapon is equipped (when speed exceeds this value, set movespeed to this value instead.)
    float jumpForce = 7.25f; // force of the jump
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


    void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        ClientSideMgr.Instance.SetClientOwnedTanc(GetComponent<NetworkObject>());

        //// make sure ground checker is slightly higher than the ray radius (otherwise sphere cast doesn't work)
        //GroundChecker.position = Vector3.up * (groundCheckRayRadius + 0.05f);
    }


    void Update()
    {
        if (!IsOwner) return;

        // jump
        if (Input.GetKeyDown(KeyCode.Space) && !inAir)
            rigidBody.AddForce(jumpForce * rigidBody.mass * Vector3.up, ForceMode.Impulse);

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
        if (Input.GetKeyDown(KeyCode.E))
            ThrowNadeRpc();

        // pickup weapon
        if (Input.GetKeyDown(KeyCode.F))
        {
            float minDot = weaponPickupAngle;

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
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        // get directional inputs
        float xMov = Input.GetAxisRaw("Horizontal");
        float zMov = Input.GetAxisRaw("Vertical");

        // get inputted movement relative to the rotator/camera rotation
        Move = (Utils.RemoveY(HorizontalRotator.forward) * zMov + HorizontalRotator.right * xMov).normalized * acceleration;

        CalculateMovement();


        // Add downward force in addition to existing gravity when jumping (more when falling).
        float x = (rigidBody.velocity.y <= 0) ? 1 : 0.7f;
        rigidBody.AddForce(Physics.gravity * x, ForceMode.Acceleration);


        if (weapons.ContainsKey(equippedWeaponSlot))
        {
            weapons[equippedWeaponSlot].transform.position = weaponSpace.transform.position;
            weapons[equippedWeaponSlot].transform.rotation = weaponSpace.transform.rotation;
        }

        GroundCheck(groundCheckRayRadius, groundCheckRayRange);
    }

    void GroundCheck(float rayRadius, float rayRange)
    {
        // start spherecast from slight above groundchecker (if origin is within radius of target, then sphere cast shits itself and dies and does not work)
        Vector3 origin = GroundChecker.position + Vector3.up * (groundCheckRayRadius + 0.05f);
        inAir = !Physics.SphereCast(origin, rayRadius, Vector3.down, out RaycastHit hit, rayRange, Utils.LayerToLayerMask(Layers.SolidGround));
    }

    // redistribute velocity
    void CalculateMovement()
    {
        Vector3 nonVerticalVelocity = new Vector3(rigidBody.velocity.x, 0, rigidBody.velocity.z);  // get velocity without y component

        // get max velocity
        float maxV = weapons.ContainsKey(equippedWeaponSlot)
            ? weapons[equippedWeaponSlot].GetComponent<WeaponBase>().MaxVelocity
            : defaultMaxVelocity;
        // if the user is trying to move and current velocity > maximum velocity:
        // prevent them from speeding up, but allow them to direct and counteract their currently high velocity
        if (Move != Vector3.zero && nonVerticalVelocity.magnitude > maxV)
        {
            float A = Vector3.SignedAngle(nonVerticalVelocity * -1, Move, new Vector3(1, 0, 1));
            float aRadian = Mathf.Abs(A / 180);

            // reduce velocity in current direction
            rigidBody.AddForce(nonVerticalVelocity.normalized * (-1 * Move.magnitude * aRadian));
        }

        float dot = Vector3.Dot(Move.normalized, nonVerticalVelocity.normalized);

        // apply new movement input
        if (inAir && dot > 0)
            rigidBody.AddForce(Move * (1 - dot)/5);
        else
            rigidBody.AddForce(Move);

        // decelleration (if not inputting movement)
        if (Move.magnitude < 0.1f && !inAir)
        {
            if (nonVerticalVelocity.magnitude < 0.1f)
            {
                rigidBody.velocity = new Vector3(0, rigidBody.velocity.y, 0);
            }
            else
            {
                rigidBody.AddForce(new Vector3(-rigidBody.velocity.x, 0, -rigidBody.velocity.z).normalized * deceleration);
            }
        }
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
    }



    //private void OnCollisionEnter(Collision collision)
    //{
    //    if (collision.gameObject.layer == (int)Layers.SolidGround)
    //        inAir = false;
    //}

    //private void OnCollisionExit(Collision collision)
    //{
    //    if (collision.gameObject.layer == (int)Layers.SolidGround)
    //        inAir = true;
    //}

    [Rpc(SendTo.Server)]
    private void ThrowNadeRpc()
    {
        NetworkObject nadeTemp = NetworkManager.SpawnManager.InstantiateAndSpawn(nade, OwnerClientId);

        nadeTemp.GetComponent<NadeScript>().InitializeTransformRpc(VerticalRotator.transform.position + VerticalRotator.transform.forward * 2, VerticalRotator.transform.rotation);
    }
}
