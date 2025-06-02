using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Shop : MonoBehaviour
{
    [SerializeField] WeaponLookupSO weaponLookup;
    [SerializeField] List<ShopWeaponBtn> weaponBtnList;
    // Start is called before the first frame update
    void Start()
    {
        foreach (ShopWeaponBtn btn in weaponBtnList)
        {
            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                BuyWeapon(btn.weapon);
            });
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void BuyWeapon(Weapons weapon)
    {
        WeaponSlot slot = weaponLookup.Dict[weapon].GetComponent<WeaponBase>().WeaponSlot;
        ClientSideMgr.Instance.clientOwnedTanc.GetComponent<Tanc>().PickupWeaponRpc(weapon, slot);
    }
}
