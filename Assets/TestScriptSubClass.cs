using System;
using UnityEngine;

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