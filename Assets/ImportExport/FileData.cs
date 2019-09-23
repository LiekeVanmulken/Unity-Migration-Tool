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
    public class FileData
    {
        private static Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        //Has public setters because json.net won't be able to set the properties otherwise 
        [SerializeField] public string Name;
        [SerializeField] public string FileID;
        [SerializeField] public string Guid;

        [SerializeField] public MemberData[] FieldDatas;

        public FileData()
        {
        }

        public FileData(string name, string guid, string fileID = "11500000")
        {
            this.Name = name;
            this.Guid = guid;
            this.FileID = fileID;
            this.FieldDatas = GenerateFieldData(name, fileID);
        }

        private MemberData[] GenerateFieldData(string name, string fileId)
        {
            if (name.Contains("YamlTestScript"))
            {
                Debug.Log("YamlTestScript");
            }

            List<MemberData> values = new List<MemberData>();

            Type type = assemblies.SelectMany(x => x.GetTypes())
                .FirstOrDefault(x => x.FullName == name);
            if (type == null)
            {
                Debug.LogError("Could not find class of name to search for members: " + name);
                return null;
            }
            
            FieldInfo[] publicFields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            FieldInfo[] privateSerializedFields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Where(info => Attribute.IsDefined(info, typeof(SerializeField))).ToArray();

            List<MemberInfo> members = new List<MemberInfo>();
            members.AddRange(publicFields);
            members.AddRange(privateSerializedFields);

            for (var i = 0; i < members.Count; i++)
            {
                MemberInfo member = members[i];
                values.Add(new MemberData(member.Name, member.GetType()));
            }

            return values.ToArray();
        }
    }

    [Serializable]
    public class MemberData
    {
        [SerializeField] public string Name;
        [SerializeField] public Type Type;

        public MemberData()
        {
        }

        public MemberData(string name, Type type)
        {
            this.Name = name;
            this.Type = type;
        }
    }
}