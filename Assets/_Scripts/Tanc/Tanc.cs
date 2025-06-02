using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
using UnityEngine.UIElements;
using Unity.Netcode;
using Unity.VisualScripting;

public class Tanc : NetworkBehaviour
{
    // self references
    Rigidbody rigidBody;
    [SerializeField] Transform HorizontalRotator;
    [SerializeField] public Transform VerticalRotator;
    [SerializeField] Transform weaponSpace;

    // states
    float acceleration = 55; // force multiplier to acceleration force
    float deceleration = 15; // force multiplier to deceleration force
    int maxVelocity = 8; // when speed exceeds this value, set movespeed to this value isntead.
    int jumpForce = 6; // force of the jump
    public Vector3 Move { get; private set; }
    bool inAir = true;

    WeaponSlot equippedWeaponSlot;

    Dictionary<WeaponSlot, GameObject> weapons = new Dictionary<WeaponSlot, GameObject>();

    public GameObject testingprimaryweapon;
    public GameObject testingsecondaryweapon;
    public GameObject hands;

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

        PickupWeaponRpc((int)Weapons.Hands, WeaponSlot.Melee);
        EquipWeaponRpc(WeaponSlot.Melee);
        ClientSideMgr.Instance.SetClientOwnedTanc(GetComponent<NetworkObject>());
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
                    print(hit.collider.gameObject.GetComponent<WeaponBase>().weaponID);
                    PickupWeaponRpc(hit.collider.gameObject.GetComponent<WeaponBase>().weaponID, hit.collider.gameObject.GetComponent<WeaponBase>().WeaponSlot);
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

        // if falling, fall with increased (2x) gravity
        if (rigidBody.velocity.y <= 0)
            rigidBody.AddForce(Physics.gravity / 2, ForceMode.Acceleration);

        if (weapons.ContainsKey(equippedWeaponSlot))
        {
            weapons[equippedWeaponSlot].transform.position = weaponSpace.transform.position;
            weapons[equippedWeaponSlot].transform.rotation = weaponSpace.transform.rotation;
        }
    }


    void CalculateMovement()
    {
        // redistribute velocity
        Vector3 nonVerticalVelocity = new Vector3(rigidBody.velocity.x, 0, rigidBody.velocity.z);  // get velocity without y component

        // if the user is trying to move and current velocity > maximum velocity:
        // prevent them from speeding up, but allow them to direct and counteract their currently high velocity
        if (Move != Vector3.zero && nonVerticalVelocity.magnitude > maxVelocity)
        {
            float A = Vector3.SignedAngle(nonVerticalVelocity * -1, Move, new Vector3(1, 0, 1));
            float aRadian = Mathf.Abs(A / 180);

            // reduce velocity in current direction
            rigidBody.AddForce(nonVerticalVelocity.normalized * (-1 * Move.magnitude * aRadian));
        }

        // apply new movement input
        rigidBody.AddForce(Move);


        // decelleration (if not inputting movement)
        if (Move.magnitude < 0.1f)
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
        print(weapons[slot]);

        // if no weapon in that slot: do nothing
        if (!weapons.TryGetValue(slot, out GameObject fuckoff))
            return;

        // unequip current weapon
        if (weapons.TryGetValue(equippedWeaponSlot, out fuckoff))
            weapons[equippedWeaponSlot].SetActive(false);

        // equip new weapon
        equippedWeaponSlot = slot;
        weapons[equippedWeaponSlot].SetActive(true);
    }

    [Rpc(SendTo.Server)]
    public void PickupWeaponRpc(Weapons weapon, WeaponSlot slot)
    {        
        Debug.Log("PickupWeaponC2SRpc");

        weapons[slot] = NetworkManager.SpawnManager.InstantiateAndSpawn(weaponLookup.Dict[weapon], OwnerClientId).gameObject;
        weapons[slot].GetComponent<WeaponBase>().AttachedTancNetObjRef.Value = new NetworkObjectReference(NetworkObject);
    }

    /// <summary>
    /// This is called by a weapon when it recieves it's AttachedWeaponNetObjID.
    /// </summary>
    /// <param name="weapon"></param>
    /// <param name="weaponID"></param>
    /// <param name="slot"></param>
    public void Attach(GameObject weapon, int weaponID, WeaponSlot slot)
    {
        // drop weapon in the desired slot
        DropWeaponRpc(slot);

        // attach new weapon
        weapons[slot] = weapon;
        weapons[slot].transform.position += Vector3.up * 3f;
        weapons[slot].GetComponent<Rigidbody>().isKinematic = true;
        weapons[slot].GetComponent<Collider>().enabled = false;

        weapons[slot].GetComponent<WeaponBase>().AttachedTanc = this;
        weapons[slot].GetComponent<WeaponBase>().SetIsDetachedIfOwner(false);
        weapons[slot].SetActive(false);
        weapons[slot].transform.localPosition = Vector3.zero;
        weapons[slot].transform.localRotation = Quaternion.Euler(Vector3.zero);
    }

    [Rpc(SendTo.Everyone)]
    public void DropWeaponRpc(WeaponSlot slot)
    {
        // if no weapon in slot: do nothing
        if (!weapons.TryGetValue(slot, out GameObject fuckoff))
            return;

        GameObject droppedWeapon = weapons[slot];
        weapons.Remove(slot);

        droppedWeapon.transform.parent = null;
        droppedWeapon.GetComponent<WeaponBase>().AttachedTanc = null;
        droppedWeapon.GetComponent<WeaponBase>().SetIsDetachedIfOwner(true);
        droppedWeapon.transform.Rotate(-1 * droppedWeapon.transform.localRotation.eulerAngles.x, 0, 0);
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == (int)Layers.SolidGround)
            inAir = false;
    }

    private void OnCollisionExit(Collision collision)
    {

        if (collision.gameObject.layer == (int)Layers.SolidGround)
            inAir = true;
    }

    [Rpc(SendTo.Server)]
    private void ThrowNadeRpc()
    {
        NetworkObject nadeTemp = NetworkManager.SpawnManager.InstantiateAndSpawn(nade, OwnerClientId);

        nadeTemp.GetComponent<NadeScript>().InitializeTransformRpc(VerticalRotator.transform.position + VerticalRotator.transform.forward * 2, VerticalRotator.transform.rotation);
    }
}
