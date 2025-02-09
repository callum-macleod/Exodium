using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
using UnityEngine.UIElements;

public class Tanc : MonoBehaviour
{
    // self references
    Rigidbody rigidBody;
    [SerializeField] Transform HorizontalRotator;
    [SerializeField] Transform weaponSpace;

    // states
    float acceleration = 55; // force multiplier to acceleration force
    float deceleration = 15; // force multiplier to deceleration force
    int maxVelocity = 8; // when speed exceeds this value, set movespeed to this value isntead.
    int jumpForce = 6; // force of the jump
    public Vector3 Move { get; private set; }
    bool inAir = true;

    WeaponSlot equippedWeapon;

    Dictionary<WeaponSlot, GameObject> weapons = new Dictionary<WeaponSlot, GameObject>();

    public GameObject testingprimaryweapon;
    public GameObject testingsecondaryweapon;
    public GameObject hands;


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

        // testing picking up weapons
        if (Input.GetKeyDown(KeyCode.F))
            PickupWeapon(testingprimaryweapon, WeaponSlot.Primary);
        if (Input.GetKeyDown(KeyCode.G))
            PickupWeapon(testingsecondaryweapon, WeaponSlot.Secondary);
    }

    private void FixedUpdate()
    {
        // get directional inputs
        float xMov = Input.GetAxisRaw("Horizontal");
        float zMov = Input.GetAxisRaw("Vertical");

        // get inputted movement relative to the rotator/camera rotation
        Move = (Utils.RemoveY(HorizontalRotator.forward) * zMov + HorizontalRotator.right * xMov).normalized * acceleration;

        CalculateMovement();

        // if falling, fall with increased (2x) gravity
        if (rigidBody.velocity.y <= 0)
            rigidBody.AddForce(Physics.gravity, ForceMode.Acceleration);
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
        if (weapons.TryGetValue(equippedWeapon, out fuckoff))
            weapons[equippedWeapon].SetActive(false);

        // equip new weapon
        equippedWeapon = slot;
        weapons[equippedWeapon].SetActive(true);
    }

    public void PickupWeapon(GameObject weapon, WeaponSlot slot)
    {
        if (weapon.GetComponent<WeaponBase>() == null)
            throw new Exception("Tried to equip an object without a weapon component");

        weapons[slot] = Instantiate(weapon, weaponSpace.transform);
        weapons[slot].SetActive(false);
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
}
