using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public class ClientSideMgr : MonoBehaviour
{
    public static ClientSideMgr Instance;

    //private NetworkObject clientOwnedTanc;
    //public NetworkObject ClientOwnedTanc
    //{
    //    get
    //    {
    //        if (clientOwnedTanc == null) clientOwnedTanc = FindClientOwnedTanc();
    //        return clientOwnedTanc;
    //    }
    //    private set
    //    {
    //        clientOwnedTanc = value;
    //    }
    //}
    public NetworkObject ClientOwnedTanc { get; private set; }


    [SerializeField] public GameObject Shop;
    [NonSerialized] public bool ShopActive = false;


    [SerializeField] GameObject mainMenu;
    [NonSerialized] public bool MainMenuActive = false;

    public bool IsMenuActive
    {
        get
        {
            return (ShopActive || MainMenuActive);
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
    }

    void UpdateCursorLock()
    {
        if (!IsMenuActive)
            Cursor.lockState = CursorLockMode.Locked;
        else
            Cursor.lockState = CursorLockMode.None;
    }

    public void SetClientOwnedTanc(NetworkObject tanc)
    {
        ClientOwnedTanc = tanc;
    }

    //public NetworkObject FindClientOwnedTanc()
    //{
    //    Tanc[] tancs = FindObjectsByType<Tanc>(FindObjectsSortMode.InstanceID);
    //    return tancs[-1].NetworkObject;
    //}
}
