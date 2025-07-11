using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrow : MonoBehaviour
{
    [SerializeField] Rigidbody rb;

    // Update is called once per frame
    void Update()
    {
        transform.forward = rb.velocity;
    }
}
