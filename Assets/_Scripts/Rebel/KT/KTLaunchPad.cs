using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KTLaunchPad : MonoBehaviour
{
    [SerializeField] float verticalJumpStrength;
    [SerializeField] float horizontalJumpStrength;
    [SerializeField] Rigidbody _rigidbody;
    [SerializeField] CapsuleCollider trigger;
    bool trapSet = false;


    private void OnTriggerEnter(Collider other)
    {
        //if (other.gameObject.layer != (int)Layers.Rebel) return;

        Rigidbody rb = other.GetComponent<Rigidbody>();

        // velocity = current horizontal velocity + additional horizontal velocity + new vertical velocity
        rb.velocity =
            Utils.RemoveY(rb.velocity)
            + Utils.RemoveY(rb.velocity).normalized * horizontalJumpStrength
            + Vector3.up * verticalJumpStrength;

        //rb.velocity = Utils.RemoveY(rb.velocity) * horizontalJumpModifier + Vector3.up * verticalJumpStrength;

        _rigidbody.isKinematic = false;
        _rigidbody.velocity = Vector3.up * 15;
        trigger.enabled = false;

        StartCoroutine(nameof(Test));
        StartCoroutine(nameof(Despawn));
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (trapSet) return;  // we only want to do this when setting the trap (not after using trap)

        gameObject.GetComponent<Rigidbody>().isKinematic = true;
        trapSet = true;
        trigger.enabled = true;
    }

    IEnumerator Test()
    {
        yield return new WaitForSeconds(0.25f);
        _rigidbody.velocity = Vector3.down * 3;
    }

    IEnumerator Despawn()
    {
        yield return new WaitForSeconds(1.5f);
        Destroy(gameObject);
    }
}
