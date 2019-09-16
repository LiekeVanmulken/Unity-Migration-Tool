using System.Collections;
using System.Collections.Generic;
using importerexporter;
using UnityEngine;

public class FileIDCalculationScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(FileIDUtil.Compute(typeof(LibraryTestScript)));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
