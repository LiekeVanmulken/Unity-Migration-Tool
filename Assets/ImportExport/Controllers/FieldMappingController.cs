#if UNITY_EDITOR


using System.Collections.Generic;
using System.IO;
using System.Linq;
using importerexporter.controllers.customlogic;
using importerexporter.models;
using importerexporter.utility;
using UnityEngine;
using YamlDotNet.RepresentationModel;

namespace importerexporter.controllers
{
    /// <summary>
    /// Maps fields to move values.
    /// This should always be run after the transform as it assumes that the IDs have been set to the current IDs!!!
    /// </summary>
    public class FieldMappingController : Singleton<FieldMappingController>
    {
        private readonly Constants constants = Constants.Instance;
        private readonly PrefabController prefabController = PrefabController.Instance;

        /// <summary>
        /// Replaces the Fields on the MonoBehaviours according to the mergeNode data
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="foundScripts"></param>
        /// <param name="oldRootPath"></param>
        /// <param name="destinationPath"></param>
        /// <returns></returns>
        public string[]
            ReplaceFieldsByMergeNodes(string[] scene, List<FoundScript> foundScripts, string oldRootPath, string destinationPath, List<ClassModel> oldIDs, List<ClassModel> newIDs) //todo : this needs a new name!
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
                    if (scriptType.HasBeenMapped == FoundScript.MappedState.NotMapped)
                    {
                        scene = recursiveReplaceField(scene, scriptType.MergeNodes, script, foundScripts);
                    }

                    CheckCustomLogic(ref scene, document, scriptType);
                }
                else
                {
                    Debug.Log("Script found that has no mapping (No members will be replaced), Node: " +
                              document.RootNode.ToString());
                }
            }
            
//            // Copy prefabs
//            List<YamlDocument> yamlPrefabs =
//                yamlStream.Documents.Where(document => document.GetName() == "PrefabInstance").ToList();
//
//            List<PrefabModel> prefabs = prefabController.ExportPrefabs(oldRootPath);
//            foreach (YamlDocument prefabInstance in yamlPrefabs)
//            {
//                //todo : transform the prefab to change all guids and fileIDs on the prefabs
//                //todo : call this recursively on the prefab
//                
//                YamlNode sourcePrefab = prefabInstance.RootNode.GetChildren()["PrefabInstance"]["m_SourcePrefab"];
//                string guid = (string) sourcePrefab["guid"];
//                PrefabModel currentPrefab = prefabs.FirstOrDefault(prefab => prefab.Guid == guid);
//                if (currentPrefab == null)
//                {
//                    Debug.LogError("Could not find prefab for guid : " + guid);
//                }
//                prefabController.CopyPrefab(currentPrefab.Path,destinationPath);
//                
//                
////                ((MigrationWindow) MigrationWindow.Instance()).ImportTransformIDs(oldRootPath,oldIDs,newIDs,currentPrefab.Path,foundScripts);
//            }

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

                MergeNode currentMergeNode =
                    currentMergeNodes.FirstOrDefault(node => node.OriginalValue == yamlNodeKey);
                if (currentMergeNode == null)
                {
                    Debug.Log("[DataLoss] Could not find mergeNode for key : " + yamlNodeKey);
                    continue;
                }

                if (yamlNode.Value is YamlMappingNode)
                {
                    scene = handleMappingNode(scene, currentMergeNode, foundScripts, yamlNode);
                }
                else if (yamlNode.Value is YamlSequenceNode)
                {
                    scene = handleSequenceNode(scene, currentMergeNode, foundScripts, yamlNode);
                }
                else
                {
                    scene = handleValueNode(scene, currentMergeNode, yamlNodeKey, line, yamlNode);
                }
            }

            return scene;
        }

        /// <summary>
        /// Checks if there is some custom logic that can be called.
        /// And calls that logic
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="foundScripts"></param>
        /// <param name="currentMergeNode"></param>
        /// <param name="yamlNode"></param>
        /// <param name="yamlNodeKey"></param>
        /// <param name="line"></param>
        /// <returns>True when it handles logic, else false and it still needs to be handled</returns>
        private static bool CheckCustomLogic(ref string[] scene,
            YamlDocument document, FoundScript foundScript)
        {
            if (!Constants.Instance.CustomLogicMapping.ContainsKey(foundScript.newClassModel.FullName))
            {
                return false;
            }

            ICustomMappingLogic customLogic = Constants.Instance.CustomLogicMapping[foundScript.newClassModel.FullName];
            customLogic.CustomLogic(ref scene, ref document, foundScript);
            return true;
        }

        private static string[] handleValueNode(string[] scene, MergeNode currentMergeNode, string yamlNodeKey,
            int line,
            KeyValuePair<YamlNode, YamlNode> yamlNode)
        {
            if (!string.IsNullOrEmpty(currentMergeNode.NameToExportTo))
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

        private string[] handleMappingNode(string[] scene, MergeNode currentMergeNode,
            List<FoundScript> foundScripts
            , KeyValuePair<YamlNode, YamlNode> yamlNode)
        {
            var recursiveChildren = yamlNode.Value.GetChildren();
            if (recursiveChildren == null || recursiveChildren.Count == 0)
            {
                return scene;
            }

            string type = currentMergeNode.Type;
            if (string.IsNullOrEmpty(type))
            {
                Debug.LogError("Type was null for yamlKey : " + yamlNode.Key.ToString());
                return scene;
            }

            int line = yamlNode.Key.Start.Line - 1;
            scene[line] = scene[line].ReplaceFirst(currentMergeNode.OriginalValue, currentMergeNode.NameToExportTo);

            List<MergeNode> typeNodes =
                foundScripts.FirstOrDefault(script => script.oldClassModel.FullName == type)?.MergeNodes;
            if (typeNodes != null)
            {
                scene = recursiveReplaceField(scene, typeNodes, yamlNode.Value, foundScripts);
            }
            else
            {
                Debug.Log("Found a mappingNode but could not find subclasses in class : " + type);
            }

            return scene;
        }

        private string[] handleSequenceNode(string[] scene, MergeNode currentMergeNode, List<FoundScript>
            foundScripts, KeyValuePair<YamlNode, YamlNode> yamlNode)
        {
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