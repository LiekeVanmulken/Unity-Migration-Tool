using System;
using System.Collections.Generic;
using UnityEngine;

public class TestScript_Arrays : MonoBehaviour
{

    public testSubClass2[] arrayTest2;
    public List<testSubClass2> listTest2;
    public List<List<testSubClass2>> ListInListTest2;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    [Serializable]
    public class testSubClass2
    {
        
        public string test;
        public string test2;
        public testSubClass2[] testList2;

    }
}