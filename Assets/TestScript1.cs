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
}