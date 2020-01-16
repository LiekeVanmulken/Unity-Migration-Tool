#if UNITY_EDITOR || UNITY_EDITOR_BETA
using System;
using migrationtool.controllers;
using migrationtool.utility;
using migrationtool.utility.serialization;
using Newtonsoft.Json;

namespace migrationtool.models
{
    /// <summary>
    /// Data model of a class
    /// </summary>
    [JsonConverter(typeof(ClassModelConverter))]
    public class ClassModel
    {
        private readonly Constants constants = Constants.Instance;

        /// <summary>
        /// Class namespace and name
        /// </summary> 
        public string FullName;

        /// <summary>
        /// ClassName
        /// </summary>
        public string Name;

        /// <summary>
        /// Cache of the name field without capitalization
        /// </summary>
        public string NameLower;

        /// <summary>
        /// FileID of the class for when the class is a script.
        /// <remark>This can be null when the class is a non-MonoBehaviour</remark>
        /// </summary>
        public string FileID;

        /// <summary>
        /// GUID of the class for when the class is a script.
        /// <remark>This can be null when the class is a non-MonoBehaviour</remark>
        /// </summary>
        public string Guid;

        /// <summary>
        /// All fields on the class
        /// </summary>
        public FieldModel[] Fields;

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
            this.Fields = FieldGenerationController.GenerateFields(fullName);
        }

        public ClassModel(Type type, string guid, string fileID = "11500000")
        {
            initName(type.FullName);
            this.FullName = type.FullName;
            this.Guid = guid;
            this.FileID = fileID;
            this.Fields = FieldGenerationController.GenerateFields(type, 0);
        }

        public ClassModel(Type type, int iteration = 0)
        {
            string fullName = type.FullName;
            initName(fullName);

            this.FullName = fullName;
            this.Fields = FieldGenerationController.GenerateFields(type, iteration);
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
#endif