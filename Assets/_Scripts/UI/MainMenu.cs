using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class MainMenu : NetworkBehaviour
{
    // Start is called before the first frame update
    [SerializeField] Button startGameBtn;
    [SerializeField] MatchMgr matchMgr;


    public override void OnNetworkSpawn()
    {
        startGameBtn.onClick.AddListener(() =>
        {
            if (IsServer)
            {
                //matchMgr.gameObject.SetActive(true);
                matchMgr.StateMachine.UpdateCurrentState(typeof(RoundPhaseState));
            }
        });

        gameObject.SetActive(false);
    }
}
