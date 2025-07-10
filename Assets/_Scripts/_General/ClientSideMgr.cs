using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public class ClientSideMgr : MonoBehaviour
{
    public static ClientSideMgr Instance;
    public NetworkObject ClientOwnedRebel { get; private set; }


    [SerializeField] public GameObject Shop;
    [NonSerialized] public bool ShopActive = false;

    [SerializeField] public GameObject RebelSelect;
    [NonSerialized] public bool RebelSelectActive = false;

    [SerializeField] GameObject mainMenu;
    [NonSerialized] public bool MainMenuActive = false;

    public bool IsMenuActive
    {
        get
        {
            return (ShopActive || MainMenuActive || RebelSelectActive);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            ShopActive = !ShopActive;
            Shop.SetActive(ShopActive);
            UpdateCursorLock();
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            MainMenuActive = !MainMenuActive;
            mainMenu.SetActive(MainMenuActive);
            UpdateCursorLock();
        }
        if (Input.GetKeyDown(KeyCode.CapsLock))
        {
            RebelSelectActive = !RebelSelectActive;
            RebelSelect.SetActive(RebelSelectActive);
            UpdateCursorLock();
        }
    }

    void UpdateCursorLock()
    {
        if (!IsMenuActive)
            Cursor.lockState = CursorLockMode.Locked;
        else
            Cursor.lockState = CursorLockMode.None;
    }

    public void SetClientOwnedRebel(NetworkObject rebel)
    {
        ClientOwnedRebel = rebel;
    }
}
