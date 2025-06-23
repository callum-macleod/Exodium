using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AmmoDisplayMgr : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI displayText;

    [SerializeField] public TRifle gun;

    private void FixedUpdate()
    {
        if (gun != null)
            displayText.text = gun.Ammo + "/" + gun.AmmoMax;
    }
}
