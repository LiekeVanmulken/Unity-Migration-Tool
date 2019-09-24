using System.IO;
using UnityEngine;
using YamlDotNet.RepresentationModel;

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