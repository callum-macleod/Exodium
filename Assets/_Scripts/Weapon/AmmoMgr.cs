using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmmoMgr : MonoBehaviour
{
    public int ammo {  get; private set; }
    [SerializeField] private int ammoMax; public float GetAmmoMax() { return ammoMax; }

    [SerializeField] private float reloadTime; public float GetReloadTime() { return reloadTime; }
    public float reloadStartTime { get; private set; }


    private bool ReloadStarted; public bool GetReloadStarted() { return (Time.time < GetReloadEndTime()); }
    private bool ReloadDone; public bool GetReloadDone() { return ReloadDone; }


    public void ReloadStartNow()
    {
        reloadStartTime = Time.time;
        ReloadDone = false;
    }

    public float GetReloadEndTime()
    {
        return reloadStartTime + reloadTime;
    }

    public void ResetAmmo()
    {
        ammo = ammoMax;
        ReloadDone = true;
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
