using System;
using importerexporter.utility;
using Newtonsoft.Json;
using UnityEngine;

namespace importerexporter.models
{
    /// <summary>
    /// Data model of a class
    /// </summary>
    [Serializable]
    [JsonConverter(typeof(ClassModelConverter))]
    public class ClassModel
    {
        /// <summary>
        /// Class namespace and name
        /// </summary>
        [SerializeField] public string FullName;

        /// <summary>
        /// ClassName
        /// </summary>
        [SerializeField] public string Name;

        /// <summary>
        /// Cache of the name field without capitalization
        /// </summary>
        [SerializeField] public string NameLower;

        /// <summary>
        /// FileID of the class for when the class is a script.
        /// <remark>This can be null when the class is a non-MonoBehaviour</remark>
        /// </summary>
        [SerializeField] public string FileID;

        /// <summary>
        /// GUID of the class for when the class is a script.
        /// <remark>This can be null when the class is a non-MonoBehaviour</remark>
        /// </summary>
        [SerializeField] public string Guid;

        /// <summary>
        /// All fields on the class
        /// </summary>
        [SerializeField] public FieldModel[] Fields;

        public ClassModel(string fullName)
        {
            initName(fullName);
            this.FullName = fullName;
        }
        
        public ClassModel(string fullName, string guid, string fileID = "11500000")
        {
            initName(fullName);

            this.FullName = fullName;
            this.Guid = guid;
            this.FileID = fileID;
            this.Fields = FieldDataGenerationUtility.GenerateFieldData(fullName);
        }

        public ClassModel(Type type, int iteration = 0)
        {
            string fullName = type.FullName;
            initName(fullName);

            this.FullName = fullName;
            this.Fields = FieldDataGenerationUtility.GenerateFieldData(type, iteration);
        }

        /// <summary>
        /// Sets the name from the fullname
        /// </summary>
        /// <param name="fullName"></param>
        private void initName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
            {
                return;
            }
            string[] names = fullName.Split('.');
            this.Name = names[names.Length - 1];
            this.NameLower = this.Name.ToLower();
        }


        public override string ToString()
        {
            return "Name : " + FullName + "; GUID : " + Guid + "; fileID : " + FileID;
        }
    }
}