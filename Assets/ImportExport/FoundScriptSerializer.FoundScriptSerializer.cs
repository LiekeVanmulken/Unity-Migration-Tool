//using System;
//using System.Collections.Generic;
//using importerexporter.models;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
//
//namespace importerexporter
//{
//    public class FoundScriptSerializer : JsonConverter
//    {
//        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
//        {
//            FoundScript foundScript = value as FoundScript;
//
//            if (foundScript == null)
//            {
//                throw new NotImplementedException("values is not of type FoundScript");
//            }
//
//            writer.WriteStartObject();
//            writer.WritePropertyName("Class");
//            writer.WriteValue(foundScript.ClassData.Name);
//            writer = RecursiveJson(writer, foundScript.MergeNodes);
//            writer.WriteEndObject();
//        }
//
//        private JsonWriter RecursiveJson(JsonWriter writer, List<MergeNode> mergeNodes)
//        {
//            writer.WriteStartArray();
//            foreach (MergeNode mergeNode in mergeNodes)
//            {
//                 new JProperty("YamlKey",mergeNode.YamlKey);
//                
//                writer.WritePropertyName();
//                writer.WriteValue();
//
//                writer.WritePropertyName("ValueToExportTo");
//                writer.WriteValue(mergeNode.ValueToExportTo);
//
//                RecursiveJson(writer, mergeNode.MergeNodes);
//            }
//
//            writer.WriteEndArray();
//
//            return writer;
//        }
//
//        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
//            JsonSerializer serializer)
//        {
//            throw new NotImplementedException();
//        }
//
//        public override bool CanConvert(Type objectType)
//        {
//            return objectType == typeof(FoundScript);
//        }
//    }
//}