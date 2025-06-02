using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public class ClientSideMgr : MonoBehaviour
{
    public static ClientSideMgr Instance;

    public NetworkObject clientOwnedTanc { get; private set; }

    [SerializeField] GameObject shop;
    [NonSerialized] public bool shopActive = false;


    [SerializeField] GameObject myMenu;
    [NonSerialized] public bool myMenuActive = false;

    public bool IsMenuActive
    {
        get
        {
            return (shopActive || myMenuActive);
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
            shopActive = !shopActive;
            shop.SetActive(shopActive);
            UpdateCursorLock();
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            myMenuActive = !myMenuActive;
            myMenu.SetActive(myMenuActive);
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
        clientOwnedTanc = tanc;
    }
}
