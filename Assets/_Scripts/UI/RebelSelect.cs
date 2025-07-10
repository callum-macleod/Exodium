using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RebelSelect : MonoBehaviour
{
    [SerializeField] List<RebelSelectBtn> rebelSelectList;
    // Start is called before the first frame update
    void Start()
    {
        foreach (RebelSelectBtn btn in rebelSelectList)
        {
            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                SelectRebel(btn.Rebel);
            });
        }
    }

    public void SelectRebel(Rebels rebel)
    {
        ClientSideMgr.Instance.ClientOwnedRebel.GetComponent<Rebel>().rebel = rebel;
    }
}
