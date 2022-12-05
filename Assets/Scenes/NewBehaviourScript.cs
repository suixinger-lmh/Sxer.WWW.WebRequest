using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sxer.WWW.WebRequest;

public class NewBehaviourScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine( RequestUtility.Post_Data("http://172.16.210.179:8080/mips/pad/getEastMoneyCywjh", new List<UnityEngine.Networking.IMultipartFormSection>(), null));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
