using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "WeaponScriptableObject", order = 1)]
public class WeaponSO : ScriptableObject
{
    [SerializeField] private GameObject graphic;
}
