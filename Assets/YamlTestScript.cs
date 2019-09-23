using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

public class YamlTestScript : MonoBehaviour
{
    
    public string publicString;
    
    [SerializeField]
    private string privateSerializedString;
    
    private string privateUnSerializedString;
    
    
    // Start is called before the first frame update
    void Start()
    {
        // Setup the input
        var input = new StringReader(File.ReadAllText("D:\\UnityProjects\\GITHUB\\SceneImportExporter\\Assets\\Scenes\\YamlTesting.unity"));

        // Load the stream
        var yaml = new YamlStream();
        yaml.Load(input);
        Debug.Log("test");
        
//        ((YamlMappingNode)yaml.Documents[10].RootNode).Children.First().Key
        
        // Examine the stream
//        var mapping =
//            (YamlMappingNode)yaml.Documents[0].RootNode;

        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
namespace ExtensionMethods
{
    public static class MyExtensions
    {
        public static string GetName(this YamlDocument document)
        {
            return (string) ((YamlMappingNode) document.RootNode).Children.First().Key;
        }

        public static IDictionary<YamlNode, YamlNode> GetChildren(this YamlNode node)
        {
            return ((YamlMappingNode) node).Children;
        }

    }   
}
