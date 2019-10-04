using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using importerexporter.utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Object = System.Object;

namespace importerexporter.models
{
    /// <summary>
    /// Stores the data of a class that has been parsed
    /// Using in the <see cref="NewProjectImportWindow"/> and in the <see cref="OldProjectExportWindow"/>
    /// </summary>
    [Serializable]
    [JsonConverter(typeof(ClassDataConverter))]
    public class ClassData
    {
        [SerializeField] public string Name;
        [SerializeField] public string FileID;
        [SerializeField] public string Guid;
        [SerializeField] public FieldData[] FieldDatas;

        public ClassData()
        {
        }

        public ClassData(string name, string guid, string fileID = "11500000")
        {
            if (name.ToLower().Contains("testscript"))
            {
                Debug.Log(name);
            }

            this.Name = name;
            this.Guid = guid;
            this.FileID = fileID;
            this.FieldDatas = FieldDataGenerationUtility.GenerateFieldData(name);
        }

        //todo : a classdata without a guid and fileID could be potentially be very dangerous! Check if this creates new issues
        public ClassData(Type type, int iteration = 0)
        {
            this.Name = type.FullName;
            this.FieldDatas = FieldDataGenerationUtility.GenerateFieldData(type, iteration);
        }

        public override string ToString()
        {
            return "Name : " + Name + "; GUID : " + Guid + "; fileID : " + FileID;
        }

        public static List<ClassData> Parse(string json)
        {
            List<ClassData> list = new List<ClassData>();
            JArray all = JArray.Parse(json);
            foreach (JObject classData in all)
            {
                list.Add(Parse(classData));
            }

            return list;
        }

        public static ClassData Parse(JObject classData)
        {
            ClassData current = new ClassData();
            current.Name = (string) classData["Name"];
            current.Guid = (string) classData["Guid"];
            current.FileID = (string) classData["FileID"];
            
            List<FieldData> currentFields = new List<FieldData>();
            foreach (JObject field in classData["Fields"])
            {
                FieldData currentField = new FieldData();
                currentField.Name = (string) field["Name"];
                currentField.Type = new ClassData();
                currentField.Type.Name = (string) field["Name"];

                JToken classDataChild;
                if (field.TryGetValue("ClassDataFields", out classDataChild))
                {
                    JObject classDataField = (JObject) classDataChild;
                    currentField.Type = Parse(classDataField);
                }
                currentFields.Add(currentField);
            }

            current.FieldDatas = currentFields.ToArray();

            return current;
        }
    }

    public class ClassDataConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            WriteJsonRecursively(writer, (ClassData) value, serializer, 0, true);
        }

        private void WriteJsonRecursively(JsonWriter writer, ClassData classData, JsonSerializer serializer,
            int depth = 0,
            bool root = false)
        {
            if (depth >= Constants.Instance.RECURSION_DEPTH)
            {
                return;
            }

            if (!root)
            {
                writer.WritePropertyName("ClassDataFields");
            }

            writer.WriteStartObject();

            WriteKeyValue(writer, "Name", classData.Name);
            WriteKeyValue(writer, "Guid", classData.Guid);
            WriteKeyValue(writer, "FileID", classData.FileID);

            writer.WritePropertyName("Fields");
            writer.WriteStartArray();
            foreach (FieldData fieldData in classData.FieldDatas)
            {
                writer.WriteStartObject();
                WriteKeyValue(writer, "Name", fieldData.Name);
                if (fieldData.Type != null)
                {
                    WriteKeyValue(writer, "Type", fieldData.Type.Name);
                    if (fieldData.Type.FieldDatas != null)
                    {
                        WriteJsonRecursively(writer, fieldData.Type, serializer, depth + 1);
                    }
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        private void WriteKeyValue(JsonWriter writer, string key, Object value)
        {
            writer.WritePropertyName(key);
            writer.WriteValue(value);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException("Cannot deserialize this way, please use the ClassData.Parse method");
//            if (objectType != typeof(ClassData))
//            {
//                throw new NotImplementedException();
//            }
//
//            List<ClassData> classDatas = new List<ClassData>();
//            while (reader.TokenType != JsonToken.EndArray)
//            {
//                this.Next(reader);
//                classDatas.Add(ParseClassData(reader));
//                this.Next(reader);
//            }
//
//            return classDatas;
        }
//
//        private ClassData ParseClassData(JsonReader reader)
//        {
//            ClassData classData = new ClassData();
//            classData.Name = (string) readPair(reader).Value;
//            classData.Guid = (string) readPair(reader).Value;
//            classData.FileID = (string) readPair(reader).Value;
//            if (classData.Name == "TestScriptSubClass")
//            {
//                Debug.Log("test");
//            }
//
//            bool throwException = false;
//            if (throwException)
//            {
//                throw new Exception("Exit loop");
//            }
//
//            //open the array
//            this.Next(reader);
//
//            //read the "Fields" attribute of the array 
//            this.Next(reader);
//            if (reader.TokenType == JsonToken.EndArray) //fieldDatas is empty
//            {
//                Next(reader);
//                return classData;
//            }
//
//            List<FieldData> fieldDatas = new List<FieldData>();
//            bool isNext = false;
//            while (true)
//            {
//                FieldData fieldData = new FieldData();
//
//                if (!isNext)
//                {
//                    //open the object in the array
//                    this.Next(reader);
//                }
//
//
//                fieldData.Name = (string) readPair(reader).Value;
//                if (fieldData.Name == "testScriptSubClass")
//                {
//                    Debug.Log("test");
//                }
//
//                fieldData.Type = new ClassData();
//                fieldData.Type.Name = (string) readPair(reader).Value;
//                if (reader.TokenType != JsonToken.EndObject)
//                {
//                    Next(reader);
//                    Next(reader);
//                    Next(reader);
//                    fieldData.Type = ParseClassData(reader);
//                    Next(reader);
//                    Next(reader);
//                    Next(reader);
//                }
//
//                fieldDatas.Add(
//                    fieldData); //todo : something still looks to be going wrong with the ClassDataFields looks like its behind by 1 
//                Next(reader);
//                isNext = true;
////                if (reader.TokenType == JsonToken.StartObject)
////                {
////                    Next(reader);
////                }
//
//                if (reader.TokenType == JsonToken.EndArray)
//                {
//                    break;
//                }
//            }
//
//            this.Next(reader); // close the array
//            classData.FieldDatas = fieldDatas.ToArray();
//
//            return classData;
//        }
//
//        private List<string> logs = new List<string>();
//
//        private void Next(JsonReader reader)
//        {
//            string log = getReaderValue(reader);
//            logs.Add(log);
//            Debug.Log(log);
//            reader.Read();
//        }
//
//        private string getReaderValue(JsonReader reader)
//        {
//            return "Value : " + reader.Value + "; Type: " + reader.TokenType;
//        }
//
//        private KeyValuePair<string, Object> readPair(JsonReader reader)
//        {
//            string key = (string) reader.Value;
//            this.Next(reader);
//            Object Value = reader.Value;
//            this.Next(reader);
//            return new KeyValuePair<string, object>(key, Value);
//        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ClassData);
        }
    }

    [Serializable] //todo : only map fields that are defined in the yaml
    public class FieldData
    {
        private Constants constants = Constants.Instance;

        [SerializeField] public string Name;

        [SerializeField] public ClassData Type;
//        [SerializeField] public FieldData[] Children;

        public FieldData()
        {
        }

        public FieldData(string name, Type type, int iteration)
        {
            this.Name = name;
            //todo : check if this recursion still works properly!
            if (iteration > constants.RECURSION_DEPTH
                || type == typeof(string)
                || type == typeof(int)
                || type == typeof(float)
                || type == typeof(bool)
                || type == typeof(double))
            {
                this.Type = new ClassData();
                this.Type.Name = type.FullName;
                return;
            }

            this.Type = new ClassData(type, iteration);


//            this.Children = FieldDataGenerationUtility.GenerateFieldData(type, iteration);
        }
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
                type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                               BindingFlags.FlattenHierarchy);
            FieldInfo[] privateSerializedFields = type
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static |
                           BindingFlags.FlattenHierarchy)
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