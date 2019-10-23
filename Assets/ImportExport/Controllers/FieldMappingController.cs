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

        private static FieldMappingController _instance;

        private static readonly object PADLOCK = new object();

        private FieldMappingController()
        {
        }

        public static FieldMappingController Instance
        {
            get
            {
                lock (PADLOCK)
                {
                    return _instance = _instance ?? new FieldMappingController();
                }
            }
        }

        #endregion

        private readonly Constants constants = Constants.Instance;

        /// <summary>
        /// Replaces the Fields on the MonoBehaviours according to the mergeNode data
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
                    Debug.Log("Script found that has no mapping (No members will be replaced), Node: " + document.RootNode.ToString());
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
        private string[] recursiveReplaceField(string[] scene, List<MergeNode> currentMergeNodes,
            YamlNode rootYamlNode, // todo : refactor to multiple methods
            List<FoundScript> foundScripts)
        {
            IDictionary<YamlNode, YamlNode> yamlChildren = rootYamlNode.GetChildren();
            foreach (KeyValuePair<YamlNode, YamlNode> yamlNode in yamlChildren)
            {
                string yamlNodeKey = yamlNode.Key.ToString();
                if (constants.MonoBehaviourFieldExclusionList.Contains(yamlNodeKey))
                {
                    continue;
                }

                int line = yamlNode.Key.Start.Line - 1;

                if (yamlNode.Value is YamlMappingNode)
                {
                    scene = handleMappingNode(scene, currentMergeNodes, foundScripts, yamlNode, yamlNodeKey);
                }
                else if (yamlNode.Value is YamlSequenceNode)
                {
                    scene = handleSequenceNode(scene, currentMergeNodes, foundScripts, yamlNode, yamlNodeKey);
                }
                else
                {
                    scene = handleValueNode(scene, currentMergeNodes, yamlNodeKey, line, yamlNode);
                }
            }

            return scene;
        }

        private static string[] handleValueNode(string[] scene, List<MergeNode> currentMergeNodes, string yamlNodeKey,
            int
                line,
            KeyValuePair<YamlNode, YamlNode> yamlNode)
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

            return scene;
        }

        private string[] handleMappingNode(string[] scene, List<MergeNode> currentMergeNodes,
            List<FoundScript> foundScripts
            , KeyValuePair<YamlNode, YamlNode> yamlNode,
            string yamlNodeKey)
        {
            var recursiveChildren = yamlNode.Value.GetChildren();
            if (recursiveChildren == null || recursiveChildren.Count == 0)
            {
                return scene;
            }

            MergeNode currentMergeNode = currentMergeNodes.FirstOrDefault(node => node.OriginalValue == yamlNodeKey);
            string type = currentMergeNode?.Type;
            if (string.IsNullOrEmpty(type))
            {
                Debug.LogError("Type was null for yamlKey : " + yamlNodeKey);
                return scene;
            }

            List<MergeNode> typeNodes =
                foundScripts.FirstOrDefault(script => script.oldClassModel.FullName == type)?.MergeNodes;
            if (typeNodes != null)
            {
                int line = yamlNode.Key.Start.Line - 1;
                scene[line] = scene[line].ReplaceFirst(currentMergeNode.OriginalValue, currentMergeNode.NameToExportTo);

                scene = recursiveReplaceField(scene, typeNodes, yamlNode.Value, foundScripts);
            }

            else
            {
                Debug.Log("Could not find subclasses of class : " + type);
            }

            return scene;
        }

        private string[] handleSequenceNode(string[] scene, List<MergeNode> currentMergeNodes, List<FoundScript>
                foundScripts, KeyValuePair<YamlNode, YamlNode> yamlNode,
            string yamlNodeKey)
        {
            MergeNode currentMergeNode =
                currentMergeNodes.FirstOrDefault(node => node.OriginalValue == yamlNodeKey);
            if (currentMergeNode == null)
            {
                Debug.Log("Could not find current mergeNode(for list or array) of node : " + yamlNodeKey);
                return scene;
            }

            int line = yamlNode.Key.Start.Line - 1;
            scene[line] = scene[line].ReplaceFirst(currentMergeNode.OriginalValue, currentMergeNode.NameToExportTo);

            string type = currentMergeNode.Type;
            FoundScript foundScript =
                foundScripts.FirstOrDefault(script => script.oldClassModel.Name == type);
            if (foundScript == null)
            {
                Debug.Log("Could not find foundScript for MergeNode, Type : " + currentMergeNode.Type +
                          " originalValue : " +
                          currentMergeNode.OriginalValue);
                return scene;
            }

            var items = yamlNode.Value.GetItems();
            if (items == null || items.Count == 0)
            {
                Debug.Log("Array or list was null for node : " + yamlNode.Key);
                return scene;
            }

            foreach (YamlNode item in items)
            {
                scene = recursiveReplaceField(scene, foundScript.MergeNodes, item, foundScripts);
            }

            return scene;
        }
    }
}
#endif