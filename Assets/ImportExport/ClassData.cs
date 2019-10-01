using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace importerexporter
{
    /// <summary>
    /// Stores the data of a class that has been parsed
    /// Using in the <see cref="NewProjectImportWindow"/> and in the <see cref="OldProjectExportWindow"/>
    /// </summary>
    [Serializable]
    public class ClassData
    {
        //Has public setters because json.net won't be able to set the properties otherwise 
        [SerializeField] public string Name;
        [SerializeField] public string FileID;
        [SerializeField] public string Guid;

        [SerializeField] public FieldData[] FieldDatas;

        public ClassData()
        {
        }

        public ClassData(string name, string guid, string fileID = "11500000")
        {
            this.Name = name;
            this.Guid = guid;
            this.FileID = fileID;
            this.FieldDatas = FieldDataGenerationUtility.GenerateFieldData(name);
        }
    }

    [Serializable]
    public class FieldData
    {
        private Constants constants = Constants.Instance;
        
        [SerializeField] public string Name;
        [SerializeField] public string Type;
        [SerializeField] public FieldData[] Children;

        public FieldData()
        {
        }

        public FieldData(string name, Type type, int iteration)
        {
            this.Name = name;
            this.Type = type.FullName;
            if (iteration > constants.RECURSION_DEPTH)
            {
                return;
            }

            if (type == typeof(string) 
                || type == typeof(int) 
                || type == typeof(float) 
                || type == typeof(bool) 
                || type == typeof(double))
            {
                return;
            }

            this.Children = FieldDataGenerationUtility.GenerateFieldData(type, iteration);
        }

//        public string[] GenerateFieldNames()
//        {
//            return Children.Select(field => field.Name).ToArray();
//        }
    }

    public static class FieldDataGenerationUtility
    {
        private static Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        /// <summary>
        /// Gets all the fields on a class
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static FieldData[] GenerateFieldData(string name)
        {
            Type type = assemblies.SelectMany(x => x.GetTypes())
                .FirstOrDefault(x => x.FullName == name);
            if (type == null)
            {
                Debug.LogError("Could not find class of name to search for members: " + name);
                return null;
            }

            return GenerateFieldData(type, 0);
        }

        /// <summary>
        /// Gets all the fields on a class
        /// </summary>
        /// <param name="type"></param>
        /// <param name="iteration">Times it has ran, used to recursively get the children</param>
        /// <returns></returns>
        public static FieldData[] GenerateFieldData(Type type, int iteration)
        {
            iteration++;
            List<FieldData> values = new List<FieldData>();


            FieldInfo[] publicFields =
                type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            FieldInfo[] privateSerializedFields = type
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Where(info => Attribute.IsDefined(info, typeof(SerializeField))).ToArray();

            List<FieldInfo> members = new List<FieldInfo>();
            members.AddRange(publicFields);
            members.AddRange(privateSerializedFields);

            for (var i = 0; i < members.Count; i++)
            {
                FieldInfo member = members[i];
                values.Add(new FieldData(member.Name, member.FieldType, iteration));
            }

            return values.ToArray();
        }
    }
}