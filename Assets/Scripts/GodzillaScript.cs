using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GodzillaScript : MonoBehaviour
{
    string mystring = "aloooo";


    // Start is called before the first frame update
    void Start()
    {
        print(mystring);
    }

    // Update is called once per frame
    void Update()
    {
        Input.GetAxisRaw("Horizontal");
    }
}
