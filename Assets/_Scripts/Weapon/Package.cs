using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Package : WeaponBase
{
    public override WeaponSlot WeaponSlot => WeaponSlot.Package;

    public override float MaxVelocity => 9;

    protected override int baseDamage { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    protected override float critMultiplier { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    protected override float limbMultiplier { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }



    float timeOfInitiatingPlant;
    float plantingDuration = 2f;
    bool planting = false;

    float detonationTimer = 5f;
    public bool Planted { get; private set; } = false;
    float timeOfPlantCompleted;

    bool detonating = false;
    [SerializeField] float detonationDuration = 2f;
    float timeOfDetonation;

    [SerializeField] float explosionMultiplier;

    protected override void OnUpdate()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.Mouse0) && !Planted) Shoot();

        else if (planting) Planting();

        else if (Planted) WhilePlanted();

        else if (detonating) WhileDetonating();
    }



    public override void Shoot()
    {
        planting = true;
        timeOfInitiatingPlant = Time.time;
    }

    private void Planting()
    {
        if (!Input.GetKey(KeyCode.Mouse0)) planting = false;

        else if (timeOfInitiatingPlant + plantingDuration <= Time.time) FinishPlant();
    }

    private void FinishPlant()
    {
        print("planted");
        planting = false;
        Planted = true;
        timeOfPlantCompleted = Time.time;
        AttachedRebel.DropWeaponRpc(WeaponSlot);
    }

    private void WhilePlanted()
    {
        if (timeOfPlantCompleted + detonationTimer <= Time.time) Detonate();
    }

    private void Detonate()
    {
        timeOfDetonation = Time.time;
        detonating = true;
        Planted = false;
    }

    private void WhileDetonating()
    {
        if (timeOfDetonation + detonationDuration >= Time.time)
            transform.localScale = Vector3.one * (Time.time - timeOfDetonation) * explosionMultiplier;
    }
}
