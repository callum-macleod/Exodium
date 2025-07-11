using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrow : MonoBehaviour
{
    // Fake gravity. Start above 0 for some upward initial velocity (see sova arrows)
    [SerializeField] float currentDownwardVelocity = 0.5f;

    [SerializeField] Transform graphic;

    // Update is called once per frame
    void Update()
    {
        Vector3 forwardVel = (transform.forward * 50);
        Vector3 downwardVel = (currentDownwardVelocity * Vector3.down);
        transform.position += (forwardVel + downwardVel) * Time.deltaTime;

        currentDownwardVelocity += 10 * Time.deltaTime;


        graphic.forward = (forwardVel + downwardVel);

        graphic.localPosition -= graphic.localPosition * Time.deltaTime * 6;
    }
}
