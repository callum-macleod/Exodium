using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public class ClientSideMgr : MonoBehaviour
{
    public static ClientSideMgr Instance;

    public NetworkObject ClientOwnedTanc { get; private set; }

    [SerializeField] public GameObject Shop;
    [NonSerialized] public bool ShopActive = false;


    [SerializeField] GameObject myMenu;
    [NonSerialized] public bool MyMenuActive = false;

    public bool IsMenuActive
    {
        get
        {
            return (ShopActive || MyMenuActive);
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
            MyMenuActive = !MyMenuActive;
            myMenu.SetActive(MyMenuActive);
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
}
