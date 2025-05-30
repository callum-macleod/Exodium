using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button clientBtn;
    [SerializeField] private Button serverBtn;
    [SerializeField] private Button hostBtn;

    private void Awake()
    {
        clientBtn.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
        });


        serverBtn.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartServer();
        });


        hostBtn.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
        });
    }
}