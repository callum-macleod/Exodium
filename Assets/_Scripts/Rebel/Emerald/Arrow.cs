using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Diagnostics;

public class Arrow : NetworkBehaviour
{
    // Fake gravity. Start above 0 for some upward initial velocity (see sova arrows)
    [SerializeField] float currentDownwardVelocity = 0.5f;
    Vector3 currentHorizontalVel;

    [SerializeField] Transform graphic;
    [SerializeField] EmeraldKeybindsSO keybinds;

    bool guiding = true;
    float speed = 50f;
    float gravity = 10f;
    float graphicAdjustmentRate = 6f;


    public override void OnNetworkSpawn()
    {
        currentHorizontalVel = Utils.ComponentWiseMult(GetRebelHorizontal(), Vector3.forward + Vector3.right);
        currentDownwardVelocity += -GetRebelVertical().y * speed;
    }

    // Update is called once per frame
    void Update()
    {
        // stop guiding when you release the ability's keybind
        if (guiding && Input.GetKeyUp(keybinds.AbilityKeybinds[AbililtyN.Ability2])) guiding = false;

        // if guiding: calculate horizontal velocity
        if (guiding) currentHorizontalVel = (Utils.ComponentWiseMult(GetRebelHorizontal(), Vector3.forward + Vector3.right) * speed);
        // calculate vertical velocity
        Vector3 downwardVel = (currentDownwardVelocity * Vector3.down);

        // move
        transform.position += (currentHorizontalVel + downwardVel) * Time.deltaTime;
        // add gravity to downward velocity
        currentDownwardVelocity += gravity * Time.deltaTime;

        // update graphic rotation (to form a proper arrow arc)
        graphic.forward = (currentHorizontalVel + downwardVel);
        // adjust graphic to align closer to the actual projectile
        graphic.localPosition -= graphic.localPosition * Time.deltaTime * graphicAdjustmentRate;
    }

    Vector3 GetRebelHorizontal()
    {
        return ClientSideMgr.Instance.ClientOwnedRebel.GetComponent<Rebel>().HorizontalRotator.forward;
    }
    Vector3 GetRebelVertical()
    {
        return ClientSideMgr.Instance.ClientOwnedRebel.GetComponent<Rebel>().VerticalRotator.forward;
    }
}
