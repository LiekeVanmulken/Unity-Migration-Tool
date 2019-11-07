using System;
using UnityEngine;

[Serializable]
public class TestScript2 : MonoBehaviour
{
    public string testTestScript2;

    [SerializeField] private string privateTestScript2;


    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Calling from TestScript2");
    }
}