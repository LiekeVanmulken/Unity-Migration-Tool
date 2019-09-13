using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using YamlDotNet.RepresentationModel;

public class OldProjectExportWindow : EditorWindow
{
    [MenuItem("ImportExport/Old project export window")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(OldProjectExportWindow));
    }

    void OnGUI()
    {
        if (GUILayout.Button("Export current scene ID's to json"))
        {
            var rootPath = Application.dataPath.Replace("/Assets", "");
            var path = rootPath + "/" + EditorApplication.currentScene;
            Debug.Log(path);

            if (File.Exists(path))
            {
                ConvertYaml(path);
            }
            else
            {
                throw new NotImplementedException("Could not find scene with path : " + path);
            }
        }

        jsonData = GUILayout.TextArea(jsonData);
    }

    private string jsonData;

    /// <summary>
    /// Convert yaml to a FilePaths
    /// </summary>
    /// <param name="path"></param>
    private void ConvertYaml(string path)
    {
        GameObject[] gameObjects = GameObject.FindSceneObjectsOfType(typeof(GameObject)) as GameObject[];
        Array.Reverse(gameObjects);

        string text = File.ReadAllText(path);
        StringReader input = new StringReader(text);
        var yaml = new YamlStream();
        yaml.Load(input);

        List<YamlDocument> yamlDocuments = GetGameObjectYamlDocuments(yaml);

        if (yamlDocuments.Count != gameObjects.Length)
        {
            Debug.LogError("Different length game objects in the yaml and the scene");
        }
        else
        {
            Debug.Log("GameObjects match the yaml documents");
        }

        // Parse the yaml to fileDatas
        var fileDatas = ParseYaml(yaml, gameObjects, yamlDocuments);

        var json = JsonConvert.SerializeObject(fileDatas, Formatting.Indented);
        jsonData = json;
        Debug.Log(json);
    }

    /// <summary>
    /// Parses the yaml to return filePaths
    /// </summary>
    /// <param name="yaml"></param>
    /// <param name="gameObjects"></param>
    /// <param name="yamlDocuments"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private List<FileData> ParseYaml(YamlStream yaml, GameObject[] gameObjects, List<YamlDocument> yamlDocuments)
    {
        List<FileData> fileDatas = new List<FileData>();
        for (int i = 0; i < gameObjects.Length; i++)
        {
            var currentGameObject = gameObjects[i];
//            var yamlDocument = yamlDocuments[i];
            var yamlDocument = findGameObjectDocumentInYamlDocuments(yamlDocuments, currentGameObject);


            Component[] components = currentGameObject.GetComponents<Component>();
            List<string> tags = getTagsFromGameObjectComponents(yamlDocument);
            if (tags.Count != components.Length)
            {
                throw new NotImplementedException("fileIDs and components do not match");
            }

            Debug.Log("fileIDS and components matched");

            for (int j = 0; j < components.Length; j++)
            {
                Component component = components[j];
                string tag = tags[j];

                YamlDocument document = getYamlDocumentByAnchor(yaml, tag);

                if (!isMonoBehaviour(document)) {continue;}

                FoundDataWrapper scriptInfo = getGuidFromDocument(document);
                if (scriptInfo != null)
                {
                    fileDatas.Add(new FileData(component.GetType().FullName, scriptInfo.Guid,
                        scriptInfo.FileID));
                }
            }
        }

        return fileDatas;
    }

    /// <summary>
    /// Gets all the tags of the components it has in the m_Component array
    /// Basically gets the id for the components that it has 
    /// </summary>
    /// <param name="document"></param>
    /// <returns></returns>
    private List<string> getTagsFromGameObjectComponents(YamlDocument document)
    {
        List<string> tags = new List<string>();
        YamlSequenceNode componentNode = (YamlSequenceNode) document.RootNode["GameObject"]["m_Component"];
        foreach (YamlMappingNode component in componentNode)
        {
            YamlNode componentUnwrapped = component["component"];
            string tag = ((YamlScalarNode) componentUnwrapped["fileID"]).Value;
            Debug.Log("Tag : " + tag);
            tags.Add(tag);
        }

        return tags;
    }

    /// <summary>
    /// The Anchor is the same as the fileID
    /// </summary>
    /// <param name="yaml"></param>
    /// <param name="anchor"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private YamlDocument getYamlDocumentByAnchor(YamlStream yaml, string anchor)
    {
        foreach (YamlDocument document in yaml.Documents)
        {
            if (document.RootNode.Anchor.Equals(anchor))
            {
                return document;
            }
        }

        throw new NotImplementedException();
    }

    /// <summary>
    /// Data wrapper to return the found data
    /// </summary>
    private sealed class FoundDataWrapper
    {
        public string Guid { get; private set; }
        public string FileID { get; private set; }

        public FoundDataWrapper(string guid, string fileId)
        {
            Guid = guid;
            FileID = fileId;
        }
    }

    /// <summary>
    /// Gets the guid and fileID from a yaml document
    /// </summary>
    /// <param name="document"></param>
    /// <returns>item1 => guid, item2 => guid</returns>
    private FoundDataWrapper getGuidFromDocument(YamlDocument document)
    {
        try
        {
            YamlMappingNode scriptNode = (YamlMappingNode) document.RootNode["MonoBehaviour"]["m_Script"];
            string guid = ((YamlScalarNode) scriptNode["guid"]).Value;
            string fileID = "";
            try
            {
                fileID = ((YamlScalarNode) scriptNode["fileID"]).Value;
            }
            catch (Exception e)
            {
                Debug.Log("Could not find fileID");
            }

            return new FoundDataWrapper(guid, fileID);
        }
        catch (Exception e)
        {
            Debug.LogError("Could not find script for document in GetGUIDFromDocument");
            return null;
        }
    }

    /// <summary>
    /// Returns all yaml documents of type gameobject
    /// </summary>
    /// <param name="yaml"></param>
    /// <returns></returns>
    private List<YamlDocument> GetGameObjectYamlDocuments(YamlStream yaml)
    {
        List<YamlDocument> yamlDocuments = new List<YamlDocument>();
        foreach (YamlDocument document in yaml.Documents)
        {
            Debug.LogWarning(document.RootNode.Anchor + ": " + document.RootNode.Tag);

            string type = "";
            foreach (KeyValuePair<YamlNode, YamlNode> entry in (YamlMappingNode) document.RootNode)
            {
                type = ((YamlScalarNode) entry.Key).Value;
//                Debug.Log("Type : " + type);
            }

            if (type.Equals("GameObject"))
            {
                yamlDocuments.Add(document);
            }
        }

        return yamlDocuments;
    }

    /// <summary>
    /// Returns the document with the same name as the gameobject
    /// </summary>
    /// <param name="yaml"></param>
    /// <param name="gameObject"></param>
    /// <returns></returns>
    private YamlDocument findGameObjectDocumentInYamlDocuments(List<YamlDocument> documents, GameObject gameObject)
    {
        foreach (YamlDocument document in documents)
        {
            YamlScalarNode name = (YamlScalarNode) document.RootNode["GameObject"]["m_Name"];
            if (name.Value == gameObject.name)
            {
                return document;
            }
        }

        return null;
    }

    private bool isGameObject(YamlDocument document)
    {
        return isOfType("GameObject", document);
    }

    private bool isMonoBehaviour(YamlDocument document)
    {
        return isOfType("MonoBehaviour", document);
    }

    private bool isOfType(string type, YamlDocument document)
    {
        try
        {
            var value = document.RootNode[type];
            return true;
        }
        catch (Exception e)
        {
            // ignored
        }

        return false;
    }
}