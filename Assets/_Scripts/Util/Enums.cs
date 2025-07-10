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
    Rebel,
    Weapon,
    RebelHitbox,
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
    Despawn = 0,
    SetInactive,
}

public enum Rebels
{
    SKT8 = 0,
    Emerald,
}

public enum AbililtyN
{
    Ability1 = 1,
    Ability2,
    Ability3,
    Ability4,
}