using System;
using UnityEngine;

[Serializable]
public class TestScript : MonoBehaviour
{
    public string test;

    [SerializeField]
    private TestScriptSubClass testScriptSubClass = new TestScriptSubClass("a", "b");
    
    
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Calling from TestScript in the after unity fuckup project");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
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
