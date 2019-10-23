using System;
using System.Collections.Generic;
using importerexporter.models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Object = System.Object;

namespace importerexporter.utility
{
    public class ClassModelConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            WriteJsonRecursively(writer, (ClassModel) value, serializer, 0, true);
        }

        private void WriteJsonRecursively(JsonWriter writer, ClassModel classModel, JsonSerializer serializer,
            int depth = 0,
            bool root = false)
        {
            if (depth >= Constants.Instance.RECURSION_DEPTH)
            {
                return;
            }

            if (!root)
            {
                writer.WritePropertyName("ClassFields");
            }

            writer.WriteStartObject();

            WriteKeyValue(writer, "FullName", classModel.FullName);
            WriteKeyValue(writer, "Name", classModel.Name);
            WriteKeyValue(writer, "Guid", classModel.Guid);
            WriteKeyValue(writer, "FileID", classModel.FileID);

            writer.WritePropertyName("Fields");
            writer.WriteStartArray();
            if (classModel.Fields != null && classModel.Fields.Length != 0)
            {
                foreach (FieldModel fieldData in classModel.Fields)
                {
                    writer.WriteStartObject();
                    WriteKeyValue(writer, "Name", fieldData.Name);
                    if (fieldData.Type != null)
                    {
                        WriteKeyValue(writer, "Type", fieldData.Type.FullName);
                        WriteKeyValue(writer, "IsIterable", fieldData.IsIterable);
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
            if (objectType != typeof(ClassModel))
            {
                throw new NotImplementedException("Not a ClassData object");
            }
            JObject classData = JObject.Load(reader);
            return Parse(classData);

        }

        private static ClassModel Parse(JObject classData)
        {
            ClassModel current = new ClassModel((string) classData["FullName"]);
            current.NameLower = current.Name?.ToLower();
            current.Guid = (string) classData["Guid"];
            current.FileID = (string) classData["FileID"];

            List<FieldModel> currentFields = new List<FieldModel>();
            foreach (JObject field in classData["Fields"])
            {
                FieldModel currentField = new FieldModel();
                currentField.Name = (string) field["Name"];
                currentField.Type = new ClassModel((string) field["Type"]);
                currentField.IsIterable= (bool) field["IsIterable"];

                JToken classDataChild;
                if (field.TryGetValue("ClassFields", out classDataChild))
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
            return objectType == typeof(ClassModel);
        }
    }
}