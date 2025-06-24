using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmmoMgr : MonoBehaviour
{
    [NonSerialized] public int ammo;
    [SerializeField] private int ammoMax; 
    [SerializeField] private float reloadTime; 
    public float reloadStartTime { get; private set; }
    private bool reloadDone; 

    public float GetAmmoMax() { return ammoMax; }
    public float GetReloadTime() { return reloadTime; }
    public bool GetReloadDone() { return reloadDone; }
    public bool ReloadStarted() { return (Time.time < GetReloadEndTime()); }

    public void ReloadStartNow()
    {
        reloadStartTime = Time.time;
        reloadDone = false;
    }

    public float GetReloadEndTime()
    {
        return reloadStartTime + reloadTime;
    }

    public void ResetAmmo()
    {
        ammo = ammoMax;
        reloadDone = true;
    }

    public void ReduceAmmoOnce()
    {
        ammo--;
    }

    public bool IsOutOfAmmo()
    {
        return ammo <= 0;
    }
}
