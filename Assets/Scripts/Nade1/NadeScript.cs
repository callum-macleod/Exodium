using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NadeScript : MonoBehaviour
{
    [SerializeField] private GameObject explosion;


    private void Start()
    {
        GetComponent<Rigidbody>().velocity = transform.forward * 10f;
    }

    private void OnTriggerEnter(Collider other)
    {
        Instantiate(explosion, transform.position, transform.rotation);
        Destroy(this.gameObject);
    }
}
