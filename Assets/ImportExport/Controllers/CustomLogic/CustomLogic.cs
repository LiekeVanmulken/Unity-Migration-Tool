using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using importerexporter.models;
using importerexporter.utility;
using UnityEngine;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace importerexporter.controllers.customlogic
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class CustomMappingLogicAttribute : Attribute
    {
        public Type type;

        public CustomMappingLogicAttribute(Type type)
        {
            if (!typeof(ICustomMappingLogic).IsAssignableFrom(type))
            {
                throw new ArgumentException(
                    "Cannot use type that does not inherit from ICustomMappingLogic, current type : " + type.FullName);
            }

            this.type = type;
        }
    }


    public interface ICustomMappingLogic
    {
        void CustomLogic(ref YamlDocument yamlDocument, FoundScript foundScript);
    }


    public class QuaternionCustomMappingLogic : ICustomMappingLogic
    {

        public void CustomLogic(ref YamlDocument yamlDocument, FoundScript foundScript)
        {
            
            YamlNode yamlNodes = yamlDocument.RootNode.GetChildren()["MonoBehaviour"];
            YamlNode value = yamlNodes["testQuaternion"];
            

            Vector3 newValue = new Quaternion(
                float.Parse(value["x"].ToString()),
                float.Parse(value["y"].ToString()),
                float.Parse(value["z"].ToString()),
                float.Parse(value["w"].ToString())
            ).eulerAngles;
            yamlNodes.GetChildren().Remove("testQuaternion");

            var result = "{ x : " + newValue.x + ", y : " + newValue.y + ", z : " + newValue.z + " }";
            yamlNodes.GetChildren().Add(new KeyValuePair<YamlNode, YamlNode>("testQuaternion",result));
            
            var serializer = new SerializerBuilder().Build();
            string yaml = serializer.Serialize(yamlDocument.RootNode);

            
            Debug.Log(yaml);
        }
    }
}