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
        private Constants constants = Constants.Instance;

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
        /// This can be null when the class is a non-MonoBehaviour
        /// </summary>
        [SerializeField] public string FileID;

        /// <summary>
        /// GUID of the class for when the class is a script.
        /// This can be null when the class is a non-MonoBehaviour
        /// </summary>
        [SerializeField] public string Guid;

        /// <summary>
        /// Says whether it is an array or a list
        /// </summary>
        [SerializeField] public bool IsIterable;

        /// <summary>
        /// All fields on the class
        /// </summary>
        [SerializeField] public FieldModel[] Fields;

        public ClassModel(string fullName)
        {
            fullName = initList(fullName);
            initName(fullName);
            this.FullName = fullName;
        }
        
        public ClassModel(string fullName, string guid, string fileID = "11500000")
        {
            fullName = initList(fullName);
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
        /// Checks if the class is a list or array
        /// </summary>
        /// <param name="fullName"></param>
        /// <returns></returns>
        private string initList(string fullName)
        {
            // Check if its a list or array and if so use the name of the class that it holds
            Match match = constants.IsListOrArrayRegex.Match(fullName);
            if (match.Success)
            {
                IsIterable = true;
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