using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Layers
{
    Default = 0,
    TransparentFX,
    IgnoreRaycast,
    SolidGround,
    Water,
    UI,
    Tanc,
    Weapon,
    TancHitbox,
}

public enum WeaponSlot
{
    Primary = 0,
    Secondary,
    Melee,
    Package,
}

public enum Weapons
{
    Hands = 0,
    Spud,
    TRifle,
    Package,
}

public enum DeathOptions
{
    Despawn,
    SetInactive,
}