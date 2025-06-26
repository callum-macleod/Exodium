using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecoilMgr : MonoBehaviour
{
    [SerializeField] float inaccuracyScalar = 0;  // tracks the current level of inaccuracy
    [SerializeField] float inaccuracyScalarDelta = 0.05f;  // how much inaccuracyScalar is increased per shot
    [SerializeField] float maxVerticalRecoil = -10;  // the maximum level of vertical recoil (in degrees)
    [SerializeField] float horizontalDelta = 2f;  // the amount which the horizontal recoil deviates per shot (multiplied with inaccuracyScalar)
    [SerializeField] float recoilDecayRate = 0.5f;  // the rate at which inaccuracy is reduced
    [SerializeField] float movePenalty = 0.5f;  // the inaccuracy penalty incurred by moving (multiplied with current velocity)

    [Header("References")]
    [SerializeField] private WeaponBase wb;
    [NonSerialized] public Transform recoilPointer;


    public void CalculateBaseInaccuracy()
    {
        float currentHorizontal = recoilPointer.transform.localRotation.eulerAngles.y;
        float potentialDeviation = horizontalDelta * inaccuracyScalar * UnityEngine.Random.Range(-1f, 1f);  // how much to change horizontal by
        float newHor = currentHorizontal + potentialDeviation;

        recoilPointer.transform.localRotation = Quaternion.Euler(maxVerticalRecoil * inaccuracyScalar, newHor, 0);
    }

    public void UpdateWeaponSpace()
    {
        if (inaccuracyScalar > 0)  // update weaponspace to match the recoil angle
            wb.AttachedRebel.WeaponSpace.localRotation = Quaternion.Euler(
                recoilPointer.transform.localRotation.eulerAngles.x,
                recoilPointer.transform.localRotation.eulerAngles.y,
                0);
    }

    public void DecayRecoil(AmmoMgr? _am)
    {
        if (inaccuracyScalar > 0 && (!Input.GetKey(KeyCode.Mouse0) || _am?.GetReloadEndTime() > Time.time))
        {
            float multiplier = (inaccuracyScalar > 0.5f) ? 2f : 1f;
            inaccuracyScalar -= recoilDecayRate * Time.deltaTime * multiplier;  // this maybe should not be done in update / with Time.deltaTime

            // handles cases where the rotation is negative
            // e.g. a rotation of -2 will actually be stored as 358. hence, we need to force it to be -2.
            float currHor = recoilPointer.transform.localRotation.eulerAngles.y;
            if (currHor > 180) currHor -= 360;

            // reduce recoilPointer rotation
            recoilPointer.transform.localRotation = Quaternion.Euler(
                maxVerticalRecoil * inaccuracyScalar,
                currHor * inaccuracyScalar,
                0);
        }
    }

    public void AddMovementPenalty()
    {
        float currentSpeed = wb.AttachedRebel.GetComponent<Rigidbody>().velocity.magnitude;
        float xPenalty = movePenalty * currentSpeed * UnityEngine.Random.Range(-1f, 1f);
        float yPenalty = movePenalty * currentSpeed * UnityEngine.Random.Range(-1f, 1f);

        recoilPointer.transform.localRotation = Quaternion.Euler(
            recoilPointer.transform.localEulerAngles.x + xPenalty,
            recoilPointer.transform.localEulerAngles.y + yPenalty,
            recoilPointer.transform.localEulerAngles.z);
    }

    public void IncreaseInaccuracy()
    {
        // increase inaccuracy (cap at 1)
        inaccuracyScalar += inaccuracyScalarDelta;
        if (inaccuracyScalar > 1) inaccuracyScalar = 1f;
    }

    public void ResetInaccuracyToZero()
    {
        inaccuracyScalar = 0f;
    }
}
