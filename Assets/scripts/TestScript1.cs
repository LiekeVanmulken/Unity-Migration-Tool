using System;
using UnityEngine;

[Serializable]
public class TestScript1 : MonoBehaviour
{
    public string test2;
    
    [SerializeField]
    private TestScriptSubClass testScriptSubClass = new TestScriptSubClass("a", "b");

    public class testingSubClass
    {
        [SerializeField]
        private string test2;

        public string publicTest;

    }
    [Serializable]
    public class TestScriptSubClass
    {
        [SerializeField] private string privateString;
        public string publicString;

        public TestScriptSubClass(string privateString, string publicString)
        {
            this.privateString = privateString;
            this.publicString = publicString;
        }
    }
}