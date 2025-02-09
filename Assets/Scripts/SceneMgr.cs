using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneMgr : MonoBehaviour
{
    [SerializeField] GameObject SpawnPoint;
    [SerializeField] GameObject testTancPrefab;

    // Start is called before the first frame update
    void Start()
    {
        Utils.InstantiateWithOptions(testTancPrefab, SpawnPoint.transform.position, Quaternion.identity, "test tanc");
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
