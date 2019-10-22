using System;
using System.Collections.Generic;
using UnityEngine;

public class TestScript_Arrays : MonoBehaviour
{
    public testSubClass2[] arrayTest2;
    public List<testSubClass2> listTest;
    public List<List<testSubClass2>> ListInListTest;
    
    [Serializable]
    public class testSubClass2
    {
        public string test;
        public string test2;
        public testSubClass2[] testList2;
    }
}