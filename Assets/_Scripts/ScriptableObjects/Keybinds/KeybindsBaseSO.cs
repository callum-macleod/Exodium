using AYellowpaper.SerializedCollections;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeybindsBaseSO : ScriptableObject
{
    [SerializeField] public SerializedDictionary<AbilityN, KeyCode> AbilityKeybinds;
}
