using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NadeScript : MonoBehaviour
{
    [SerializeField] private GameObject explosion;

    private void OnTriggerEnter(Collider other)
    {
        Instantiate(explosion, transform.position, transform.rotation);
        Destroy(this.gameObject);
    }
}
