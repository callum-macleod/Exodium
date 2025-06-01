using AYellowpaper.SerializedCollections;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponLookup", menuName = "ScriptableObjects/WeaponLookup", order = 1)]
public class WeaponLookupSO : ScriptableObject
{
    [SerializeField] public SerializedDictionary<int, NetworkObject> Dict;
}
