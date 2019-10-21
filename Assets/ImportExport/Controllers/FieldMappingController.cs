#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using System.Linq;
using importerexporter.models;
using importerexporter.utility;
using UnityEngine;
using YamlDotNet.RepresentationModel;

namespace importerexporter
{
    /// <summary>
    /// Maps fields to move values.
    /// This should always be run after the transform as it assumes that the IDs have been set to the current IDs!!!
    /// </summary>
    public class FieldMappingController
    {
        #region Singleton

        private static FieldMappingController instance = null;

        private static readonly object padlock = new object();

        FieldMappingController()
        {
        }

        public static FieldMappingController Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new FieldMappingController();
                    }

                    return instance;
                }
            }
        }

        #endregion

        private Constants constants = Constants.Instance;

        /// <summary>
        /// Replaces the Fields on the monobehaviours according to the mergeNode data
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="foundScripts"></param>
        /// <returns></returns>
        public string[]
            ReplaceFieldsByMergeNodes(string[] scene, List<FoundScript> foundScripts) //todo : this needs a new name!
        {
            string sceneContent = string.Join("\n", scene);

            YamlStream yamlStream = new YamlStream();
            yamlStream.Load(new StringReader(sceneContent));
            List<YamlDocument> yamlDocuments =
                yamlStream.Documents.Where(document => document.GetName() == "MonoBehaviour").ToList();
            foreach (YamlDocument document in yamlDocuments)
            {
                YamlNode script = document.RootNode.GetChildren()["MonoBehaviour"]; //todo : duplicate code, fix 

                string fileID = (string) script["m_Script"]["fileID"];
                string guid = (string) script["m_Script"]["guid"];

                FoundScript scriptType =
                    foundScripts.FirstOrDefault(node =>
                        node.newClassModel.Guid == guid && node.newClassModel.FileID == fileID);
                if (scriptType != null)
                {
                    scene = recursiveReplaceField(scene, scriptType.MergeNodes, script, foundScripts);
                }
                else
                {
                    Debug.Log("Script found but no mapping for " + document.RootNode.ToString());
                }
            }

            return scene;
        }

        /// <summary>
        /// Helper method for the<see cref="ReplaceFieldsByMergeNodes"/> to replace the fields in the scripts.
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="currentMergeNodes"></param>
        /// <param name="rootYamlNode"></param>
        /// <returns></returns>
        private string[] recursiveReplaceField(string[] scene, List<MergeNode> currentMergeNodes, YamlNode rootYamlNode, // todo : refactor to multiple methods
            List<FoundScript> foundScripts)
        {
            IDictionary<YamlNode, YamlNode> yamlChildren = rootYamlNode.GetChildren();
            foreach (KeyValuePair<YamlNode, YamlNode> yamlNode in yamlChildren)
            {
                string yamlNodeKey = (string) yamlNode.Key;
                if (constants.MonoBehaviourFieldExclusionList.Contains(yamlNode.Key.ToString()))
                {
                    continue;
                }

                int line = yamlNode.Key.Start.Line - 1;
                switch (yamlNode.Value)
                {
                    case YamlMappingNode _:
                    {
                        var recursiveChildren = yamlNode.Value.GetChildren();
                        if (recursiveChildren == null || recursiveChildren.Count == 0)
                        {
                            continue;
                        }
                        //todo : the parent of a children doesn't get changed

                        string type = currentMergeNodes.FirstOrDefault(node => node.OriginalValue == yamlNodeKey)?.Type;
                        if (string.IsNullOrEmpty(type))
                        {
                            Debug.LogError("Type was null for yamlKey : " + yamlNodeKey);
                            continue;
                        }

                        List<MergeNode> typeNodes =
                            foundScripts.FirstOrDefault(script => script.oldClassModel.FullName == type)?.MergeNodes;
                        if (typeNodes != null)
                        {
                            scene = recursiveReplaceField(scene, typeNodes, yamlNode.Value, foundScripts);
                        }
                        else
                        {
                            Debug.Log("Could not find subclasses of class : " + type);
                        }

                        break;
                    }
                    case YamlSequenceNode _:
                    {
                        var items = yamlNode.Value.GetItems();
                        if (items == null || items.Count == 0)
                        {
                            Debug.Log("Array or list was null for node : " + yamlNode.Key);
                            continue;
                        }

                        MergeNode currentMergeNode =
                            currentMergeNodes.FirstOrDefault(node => node.OriginalValue == yamlNodeKey);
                        if (currentMergeNode == null)
                        {
                            Debug.Log("Could not find current mergeNode(for list or array) of node : " + yamlNodeKey);
                            continue;
                        }

                        FoundScript foundScript =
                            foundScripts.FirstOrDefault(script => script.oldClassModel.Name == currentMergeNode.Type);
                        if (foundScript == null)
                        {
                            Debug.Log("Could not find foundScript for MergeNode, Type : " + currentMergeNode.Type + " originalValue : " + currentMergeNode.OriginalValue);
                            continue;
                        }

                        foreach (YamlNode item in items)
                        {
                            scene = recursiveReplaceField(scene, foundScript.MergeNodes, item, foundScripts);
                        }

                        break;
                    }
                    default:
                    {
                        MergeNode currentMergeNode =
                            currentMergeNodes.FirstOrDefault(node => node.OriginalValue == yamlNodeKey);

                        if (currentMergeNode != null && !string.IsNullOrEmpty(currentMergeNode.NameToExportTo))
                        {
                            scene[line] = scene[line]
                                .ReplaceFirst(currentMergeNode.OriginalValue, currentMergeNode.NameToExportTo);
                        }
                        else
                        {
                            Debug.Log("Mapping failed for : " + yamlNodeKey + " node : " + yamlNode.ToString());
                        }

                        break;
                    }
                }
            }

            return scene;
        }
    }
}
#endif