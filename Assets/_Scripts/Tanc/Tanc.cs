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

    private void Start()
    {
        PickupWeapon(hands, WeaponSlot.Melee);
        EquipWeapon(WeaponSlot.Melee);
    }


    void Update()
    {
        if (!IsOwner) return;

        // jump
        if (Input.GetKeyDown(KeyCode.Space) && !inAir)
            rigidBody.AddForce(jumpForce * rigidBody.mass * Vector3.up, ForceMode.Impulse);

        // equip weapons
        if (Input.GetKeyDown(KeyCode.Alpha1))
            EquipWeapon(WeaponSlot.Primary);
        if (Input.GetKeyDown(KeyCode.Alpha2))
            EquipWeapon(WeaponSlot.Secondary);
        if (Input.GetKeyDown(KeyCode.Alpha3))
            EquipWeapon(WeaponSlot.Melee);

        // drop weapon
        if (Input.GetKeyDown(KeyCode.G))
            DropWeapon(equippedWeaponSlot);

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
                    PickupWeapon(hit.collider.gameObject, hit.collider.gameObject.GetComponent<WeaponBase>().WeaponSlot);
                }
            }
        }

        // testing with prefabs
        if (Input.GetKeyDown(KeyCode.F1))
            PickupWeapon(testingprimaryweapon, WeaponSlot.Primary);
        if (Input.GetKeyDown(KeyCode.F2))
            PickupWeapon(testingsecondaryweapon, WeaponSlot.Secondary);
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
            rigidBody.AddForce(new Vector3(-rigidBody.velocity.x, 0, -rigidBody.velocity.z).normalized * deceleration);
    }


    public void EquipWeapon(WeaponSlot slot)
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
    }

    public void PickupWeapon(GameObject weapon, WeaponSlot slot)
    {
        if (weapon.GetComponent<WeaponBase>() == null)
            throw new Exception("Tried to equip an object without a weapon component");
        
        // drop weapon in the desired slot
        DropWeapon(slot);

        WeaponBase weaponBase = weapon.GetComponent<WeaponBase>();
        if (IsServer)
        {
            weapons[slot] = NetworkManager.SpawnManager.InstantiateAndSpawn(weaponLookup.Dict[weaponBase.weaponID], OwnerClientId).gameObject;
            weapons[slot].GetComponent<WeaponBase>().AttachedTancNetObjID.Value = NetworkObjectId;

            //weapons[slot].transform.position += Vector3.up * 3f;
            //weapons[slot].GetComponent<Rigidbody>().isKinematic = true;
            //weapons[slot].GetComponent<Collider>().enabled = false;

            //weapons[slot].GetComponent<WeaponBase>().AttachedTanc = this;
            //weapons[slot].GetComponent<WeaponBase>().IsDetached = false;
            //weapons[slot].GetComponent<WeaponBase>().StartAsDetached = false;
            //weapons[slot].SetActive(false);
            //weapons[slot].transform.localPosition = Vector3.zero;
            //weapons[slot].transform.localRotation = Quaternion.Euler(Vector3.zero);
            //print(weapons[slot].GetComponent<WeaponBase>().AttachedTanc);
        }
        else
        {
            // this is copy and paste, please make this more efficient

            PickupWeaponC2SRpc(weaponBase.weaponID, OwnerClientId, slot);
        }
    }

    [Rpc(SendTo.Server)]
    private void PickupWeaponC2SRpc(int weaponID, ulong ownerClientID, WeaponSlot slot)
    {
        Debug.Log("PickupWeaponC2SRpc");
        weapons[slot] = NetworkManager.SpawnManager.InstantiateAndSpawn(weaponLookup.Dict[weaponID], OwnerClientId).gameObject;
        weapons[slot].GetComponent<WeaponBase>().AttachedTancNetObjID.Value = NetworkObjectId;

        //weapons[slot].transform.position += Vector3.up * 3f;
        //weapons[slot].GetComponent<Rigidbody>().isKinematic = true;
        //weapons[slot].GetComponent<Collider>().enabled = false;

        //weapons[slot].GetComponent<WeaponBase>().AttachedTanc = this;
        //weapons[slot].GetComponent<WeaponBase>().IsDetached = false;
        //weapons[slot].GetComponent<WeaponBase>().StartAsDetached = false;
        //weapons[slot].SetActive(false);
        //weapons[slot].transform.localPosition = Vector3.zero;
        //weapons[slot].transform.localRotation = Quaternion.Euler(Vector3.zero);
        //print(weapons[slot].GetComponent<WeaponBase>().AttachedTanc);

        //// tell clients to do the same
        //PickupWeaponS2CRpc(weaponID, ownerClientID, slot);
    }

    //[Rpc(SendTo.NotServer)]
    //private void PickupWeaponS2CRpc(int weaponID, ulong ownerClientID, WeaponSlot slot)
    //{
    //    Debug.Log("PickupWeaponS2CRpc");
    //    weapons[slot] = NetworkManager.SpawnManager.InstantiateAndSpawn(weaponLookup.Dict[weaponID], OwnerClientId).gameObject;
    //    //weapons[slot].transform.position += Vector3.up * 3f;
    //    //weapons[slot].GetComponent<Rigidbody>().isKinematic = true;
    //    //weapons[slot].GetComponent<Collider>().enabled = false;

    //    //weapons[slot].GetComponent<WeaponBase>().AttachedTanc = this;
    //    //weapons[slot].GetComponent<WeaponBase>().IsDetached = false;
    //    //weapons[slot].GetComponent<WeaponBase>().StartAsDetached = false;
    //    //weapons[slot].SetActive(false);
    //    //weapons[slot].transform.localPosition = Vector3.zero;
    //    //weapons[slot].transform.localRotation = Quaternion.Euler(Vector3.zero);
    //    print(weapons[slot].GetComponent<WeaponBase>().AttachedTanc);
    //}

    public void Attach(GameObject weapon, int weaponID, WeaponSlot slot)
    {
        weapons[slot] = weapon;
        weapons[slot].transform.position += Vector3.up * 3f;
        weapons[slot].GetComponent<Rigidbody>().isKinematic = true;
        weapons[slot].GetComponent<Collider>().enabled = false;

        weapons[slot].GetComponent<WeaponBase>().AttachedTanc = this;
        weapons[slot].GetComponent<WeaponBase>().IsDetached = false;
        weapons[slot].GetComponent<WeaponBase>().StartAsDetached = false;
        weapons[slot].SetActive(false);
        weapons[slot].transform.localPosition = Vector3.zero;
        weapons[slot].transform.localRotation = Quaternion.Euler(Vector3.zero);
    }

    public void DropWeapon(WeaponSlot slot)
    {
        // if no weapon in slot: do nothing
        if (!weapons.TryGetValue(slot, out GameObject fuckoff))
            return;

        GameObject droppedWeapon = weapons[slot];
        weapons.Remove(slot);

        droppedWeapon.transform.parent = null;
        droppedWeapon.GetComponent<WeaponBase>().AttachedTanc = null;
        droppedWeapon.GetComponent<WeaponBase>().IsDetached = true;
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
