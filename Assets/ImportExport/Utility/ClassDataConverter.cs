using System;
using System.Collections.Generic;
using importerexporter.models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace importerexporter.utility
{
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

            WriteKeyValue(writer, "Name", classData.FullName);
            WriteKeyValue(writer, "Guid", classData.Guid);
            WriteKeyValue(writer, "FileID", classData.FileID);

            writer.WritePropertyName("Fields");
            writer.WriteStartArray();
            if (classData.Fields != null && classData.Fields.Length != 0)
            {
                foreach (FieldData fieldData in classData.Fields)
                {
                    writer.WriteStartObject();
                    WriteKeyValue(writer, "Name", fieldData.Name);
                    if (fieldData.Type != null)
                    {
                        WriteKeyValue(writer, "Type", fieldData.Type.FullName);
                        if (fieldData.Type.Fields != null)
                        {
                            WriteJsonRecursively(writer, fieldData.Type, serializer, depth + 1);
                        }
                    }

                    writer.WriteEndObject();
                }
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
            if (objectType == typeof(ClassData))
            {
                JObject classData = JObject.Load(reader);
                return Parse(classData);
            }

            throw new NotImplementedException("Not a ClassData object");
        }
        public static ClassData Parse(JObject classData)
        {
            ClassData current = new ClassData();
            current.FullName = (string) classData["Name"];
            current.Guid = (string) classData["Guid"];
            current.FileID = (string) classData["FileID"];

            List<FieldData> currentFields = new List<FieldData>();
            foreach (JObject field in classData["Fields"])
            {
                FieldData currentField = new FieldData();
                currentField.Name = (string) field["Name"];
                currentField.Type = new ClassData();
                currentField.Type.FullName = (string) field["Type"];

                JToken classDataChild;
                if (field.TryGetValue("ClassDataFields", out classDataChild))
                {
                    JObject classDataField = (JObject) classDataChild;
                    currentField.Type = Parse(classDataField);
                }

                currentFields.Add(currentField);
            }

            current.Fields = currentFields.ToArray();

            return current;
        }
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ClassData);
        }
    }
}