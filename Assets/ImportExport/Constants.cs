using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Constants
{
    private static Constants instance = null;
    private static readonly object padlock = new object();
    public bool Debug = true;
    Constants()
    {
    }

    public static Constants Instance
    {
        get
        {
            lock (padlock)
            {
                if (instance == null)
                {
                    instance = new Constants();
                }

                return instance;
            }
        }
    }
}