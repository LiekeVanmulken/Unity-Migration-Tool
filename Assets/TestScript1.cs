using System;
using UnityEngine;

[Serializable]
public class TestScript1 : MonoBehaviour
{
    public string test;
    
    [SerializeField]
    private TestScriptSubClass testScriptSubClass = new TestScriptSubClass("a", "b");
    
    
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Calling from TestScript ");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public class testingSubClass
    {
        [SerializeField]
        private string test;

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