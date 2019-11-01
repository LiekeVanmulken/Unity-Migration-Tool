using importerexporter.models;
using importerexporter.utility;
using UnityEngine;
using YamlDotNet.RepresentationModel;

namespace importerexporter.controllers.customlogic
{
    public class QuaternionCustomMappingLogic : ICustomMappingLogic
    {
        public void CustomLogic(ref string[] scene, ref YamlDocument yamlDocument, FoundScript foundScript)
        {
            // Get all values
            YamlNode yamlNodes = yamlDocument.RootNode.GetChildren()["MonoBehaviour"];
            
            // Get the value we wish to change, this is the originalValue and has not been changed in the yaml document
            // That's why we need to use the original name to transform the data
            YamlNode originalValue = yamlNodes["testQuaternion"];
            int line = originalValue.Start.Line - 1;

            // Transform the data
            Vector3 newValue = new Quaternion(
                float.Parse(originalValue["x"].ToString()),
                float.Parse(originalValue["y"].ToString()),
                float.Parse(originalValue["z"].ToString()),
                float.Parse(originalValue["w"].ToString())
            ).eulerAngles;

            // Make the value we want to set
            string valueString = "{ x : " + newValue.x + ", y : " + newValue.y + ", z : " + newValue.z +" }";
            
            // Get the key that is in the newest version of the scene (this is the changed value so testQuaternion2)
            // When setting values be careful with indenting as yaml uses indents for structure 
            // Indents can be set by adding "\t" and new lines can be added by using "\r\n"
            string original = scene[line].Substring(0, scene[line].IndexOf(':'));
            
            // Replace it in the original file 
            scene[line]= original + ": " + valueString;
        }
    }
}