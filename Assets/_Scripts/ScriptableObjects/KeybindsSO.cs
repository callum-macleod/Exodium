using AYellowpaper.SerializedCollections;
using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

[CreateAssetMenu(fileName = "Keybinds", menuName = "ScriptableObjects/Keybinds/Keybinds", order = 1)]
public class KeybindsSO : ScriptableObject
{
    [SerializeField] public SerializedDictionary<Rebels, KeybindsBaseSO> Keybinds;


    //public Dictionary<Rebels, string>  SOKey = new Dictionary<Rebels, string>()
    //{
    //    {Rebels.SKT8,  nameof(SKT8KeybindsSO)},
    //    {Rebels.Emerald,  nameof(EmeraldKeybindsSO)},
    //};

    //[SerializeField] public SKT8KeybindsSO SKT8Keybinds;
    //[SerializeField] public EmeraldKeybindsSO EmeraldKeybinds;
}
