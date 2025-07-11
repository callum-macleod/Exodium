using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EmeraldArrowExplosion : NetworkBehaviour
{
    float timeOfDetonation;
    bool detonating;
    [SerializeField] float detonationDuration;
    [SerializeField] float explosionMultiplier;

    public override void OnNetworkSpawn()
    {
        Detonate();
    }

    private void Update()
    {
        if (detonating) WhileDetonating();
        else NetworkObject.Despawn();
    }

    private void Detonate()
    {
        timeOfDetonation = Time.time;
        detonating = true;
    }

    private void WhileDetonating()
    {
        if (timeOfDetonation + detonationDuration >= Time.time)
            transform.localScale =
                Vector3.one
                * ((Time.time - timeOfDetonation) / detonationDuration)
                * explosionMultiplier;

        else detonating = false;
    }
}
