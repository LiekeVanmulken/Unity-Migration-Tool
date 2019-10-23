using System;
using System.Collections.Generic;
using importerexporter.models;
using UnityEngine;
using YamlDotNet.RepresentationModel;

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
        YamlNode CustomLogic(KeyValuePair<YamlNode, YamlNode> yamlNode, List<FoundScript> foundScripts);
    }


    public class TestCustomMappingLogic : ICustomMappingLogic
    {
        public string fieldThatHasCustomData = "testQuaternion";

        public YamlNode CustomLogic(KeyValuePair<YamlNode, YamlNode> yamlNode, List<FoundScript> foundScripts)
        {
            YamlNode key = yamlNode.Key;
            YamlNode value = yamlNode.Value;

            Vector3 newValue = new Quaternion(
                float.Parse(value["x"].ToString()),
                float.Parse(value["y"].ToString()),
                float.Parse(value["z"].ToString()),
                float.Parse(value["w"].ToString())
            ).eulerAngles;

            return "{ x : " + newValue.x + ", y : " + newValue.y + ", z : " + newValue.z + " }";
        }
    }
}