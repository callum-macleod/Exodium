using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DisableIfNotOwner : NetworkBehaviour
{
    public List<GameObject> objectsToDisable;

    // Start is called before the first frame update
    void Start()
    {
        if (IsOwner) return;

        foreach (GameObject go in objectsToDisable)
        {
            go.SetActive(false);
        }
    }

}
