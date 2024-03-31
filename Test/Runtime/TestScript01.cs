using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GSTool.Array;

public class TestScript01 : MonoBehaviour
{
    public string[] infoList;
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(infoList.ToOneString());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
