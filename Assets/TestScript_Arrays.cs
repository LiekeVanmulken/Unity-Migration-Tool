using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{

    public List<testSubClass> listTest2;
    public testSubClass[] arrayTest2;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    [Serializable]
    public class testSubClass
    {
        
        public string test2;
    }
}