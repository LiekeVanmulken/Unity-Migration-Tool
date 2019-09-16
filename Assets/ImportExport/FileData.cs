using System;
using UnityEngine;

namespace ImportExporter
{
    /// <summary>
    /// Stores the data of a class that has been parsed
    /// Using in the <see cref="NewProjectImportWindow"/> and in the <see cref="OldProjectExportWindow"/>
    /// </summary>
    [Serializable]
    public class FileData
    {
        //Has public setters because json.net won't be able to set the properties otherwise 
        [SerializeField] public string Name;
        [SerializeField] public string FileID;
        [SerializeField] public string Guid;

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
}