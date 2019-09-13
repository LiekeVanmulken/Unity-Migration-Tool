using System;
using UnityEngine;

[Serializable]
public class FileData
{
    [SerializeField] public string Name { get; private set; }

    [SerializeField] public string FileID { get; private set; }
    [SerializeField] public string Guid { get; private set; }

    public FileData()
    {
    }

    public FileData(string name, string guid, string fileID = "11500000")
    {
        this.Name = name;
        this.Guid = guid;
        this.FileID = fileID;
    }
}