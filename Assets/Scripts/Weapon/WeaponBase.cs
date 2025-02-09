using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    protected abstract int baseDamage { get; set; }
    protected abstract float critMultiplier { get; set; }
    protected abstract float limbMultiplier { get; set; }


    void Start() => OnStart();
    protected abstract void OnStart();

    void Update() => OnUpdate();
    protected abstract void OnUpdate();

    void FixedUpdate() => OnFixedUpdate();
    protected abstract void OnFixedUpdate();


    public abstract void Shoot();
}
