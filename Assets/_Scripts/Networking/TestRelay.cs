using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System;
using TMPro;

public class TestRelay : MonoBehaviour
{
    public TMP_Text lobbyCodeDisplay;

    private string currentJoiningCode;

    public event EventHandler<HostStartedEventArgs> HostStarted;

    public void setCurrentJoiningCode(string _newVal)
    {
        currentJoiningCode = _newVal;
    }

    private async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed In" + AuthenticationService.Instance.PlayerId);
        };

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        AuthenticationService.Instance.SignInAnonymouslyAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    }

    public async void CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(10);

            string joincode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
                );

            NetworkManager.Singleton.StartHost();

            lobbyCodeDisplay.text = joincode;


            HostStarted += NetworkSpawner.Instance.Test;
            HostStarted?.Invoke(this, new HostStartedEventArgs("kal", joincode));
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }

    }

    public async void JoinRelay()
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(currentJoiningCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
                );

            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }
}



public class HostStartedEventArgs : EventArgs
{
    public HostStartedEventArgs(string host, string code)
    {
        HostName = host;
        JoinCode = code;
    }

    public string HostName;
    public string JoinCode;
}