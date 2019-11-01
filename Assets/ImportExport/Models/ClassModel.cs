using System;
using System.Text.RegularExpressions;
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
        private readonly Constants constants = Constants.Instance;

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
            fullName = initList(fullName);
            initName(fullName);

            this.FullName = fullName;
            this.Fields = FieldDataGenerationUtility.GenerateFieldData(type, iteration);
        }

        /// <summary>
        /// Check if its an array or list and if it it get the containing class
        /// This is so we can get the fields of the containing class as a list or array won't have fields
        /// </summary>
        /// <param name="fullName"></param>
        /// <returns></returns>
        private string initList(string fullName)
        {
            // Check if its a list or array and if so use the name of the class that it holds
            Match match = constants.IsListOrArrayRegex.Match(fullName);
            if (match.Success)
            {
                fullName = match.Value;
            }

            return fullName;
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