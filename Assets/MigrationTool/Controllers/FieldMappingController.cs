

using Newtonsoft.Json;
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using migrationtool.controllers.customlogic;
using migrationtool.models;
using migrationtool.utility;
using migrationtool.views;
using UnityEngine;
using YamlDotNet.RepresentationModel;

namespace migrationtool.controllers
{
    /// <summary>
    /// Maps fields to move values.
    /// This should always be run after the transform as it assumes that the IDs have been set to the current IDs!!!
    /// </summary>
    public class FieldMappingController
    {
        private readonly Constants constants = Constants.Instance;
        private readonly PrefabController prefabController = new PrefabController();

        /// <summary>
        /// Migrate the fields to the new version of the field
        /// Works for both scenes and prefabs. 
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="foundScripts"></param>
        /// <param name="oldRootPath"></param>
        /// <param name="destinationPath"></param>
        /// <returns></returns>
        public string[]
            MigrateFields(string scenePath, ref string[] scene, List<FoundScript> foundScripts, string oldRootPath,
                string destinationPath)
        {
            string sceneContent = string.Join("\n", scene.PrepareSceneForYaml());

            YamlStream yamlStream = new YamlStream();
            yamlStream.Load(new StringReader(sceneContent));
            
            int amountOfPrefabs =
                yamlStream.Documents.Count(document => document.GetName() == "PrefabInstance");
            if (amountOfPrefabs > 0)
            {
                new PrefabView().ParsePrefabsInAScene(scenePath, oldRootPath, destinationPath);
                
                
                //Deserialize the foundScripts
                if (File.Exists(destinationPath + constants.RelativeFoundScriptPath))
                {
                    foundScripts = JsonConvert.DeserializeObject<List<FoundScript>>(
                        File.ReadAllText(destinationPath + constants.RelativeFoundScriptPath));
                }
                    
                ConvertPrefabsDataInScene(ref scene, oldRootPath, yamlStream, foundScripts);
            }
            
            ConvertScene(ref scene, foundScripts, yamlStream);
            return scene;
        }

        private void ConvertScene(ref string[] scene, List<FoundScript> foundScripts, YamlStream yamlStream)
        {
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
                        scene = recursiveReplaceField(ref scene, scriptType.MergeNodes, script, foundScripts);
                    }

                    CheckCustomLogic(ref scene, document, scriptType);
                }
                else
                {
                    Debug.Log("Script found that has no mapping (No members will be replaced), Node: " +
                              document.RootNode.ToString());
                }
            }
        }

        private void ConvertPrefabsDataInScene(ref string[] scene, string oldRootPath, YamlStream yamlStream,
            List<FoundScript> foundScripts)
        {
            // Copy prefabs
            List<YamlDocument> yamlPrefabs =
                yamlStream.Documents.Where(document => document.GetName() == "PrefabInstance").ToList();

            List<PrefabModel> oldPrefabs = prefabController.ExportPrefabs(oldRootPath);
            foreach (YamlDocument prefabInstance in yamlPrefabs)
            {
                //Get the prefab file we're working with
                string prefabGuid = (string) prefabInstance.RootNode["PrefabInstance"]["m_SourcePrefab"]["guid"];
                PrefabModel prefabModel = oldPrefabs.First(prefabFile => prefabFile.Guid == prefabGuid);

                //Load in the prefab file
                YamlStream prefabStream = new YamlStream();
                string[] lines = File.ReadAllLines(prefabModel.Path).PrepareSceneForYaml();
                prefabStream.Load(new StringReader(string.Join("\r\n",lines)));


                //Get the modifications that have been done
                YamlSequenceNode modifications =
                    (YamlSequenceNode) prefabInstance.RootNode["PrefabInstance"]["m_Modification"]["m_Modifications"];

                //change the modifications
                foreach (YamlNode modification in modifications)
                {
                    YamlNode target = modification["target"];
                    string fileID = (string) target["fileID"];

                    string propertyPath = (string) modification["propertyPath"];

                    YamlDocument scriptReference =
                        prefabStream.Documents.First(document =>
                            document.RootNode.Anchor == fileID);
                    if (scriptReference.GetName() != "MonoBehaviour")
                    {
                        continue;
                    }

                    YamlNode IDs = scriptReference.RootNode["MonoBehaviour"]["m_Script"];

                    string scriptGuid = (string) IDs["guid"];
                    string scriptFileID = (string) IDs["fileID"];

                    FoundScript scriptType =
                        foundScripts.FirstOrDefault(node =>
                            node.oldClassModel.Guid == scriptGuid && node.oldClassModel.FileID == scriptFileID);
                    if (scriptType == null)
                    {
//                        Debug.Log("Could not find mapping for guid: " + scriptGuid + " fileID: " + scriptFileID);
                        continue;
                    }

                    string[] properties = propertyPath.Split('.');
                    List<MergeNode> currentMergeNodes = scriptType.MergeNodes;

                    for (var i = 0; i < properties.Length; i++)
                    {
                        string property = properties[i];
                        if (property == "Array" && properties.Length > i + 1 && properties[i + 1].StartsWith("data["))
                        {
                            // this is a list or array and can be skipped;
                            i++;
                            continue;
                        }


                        MergeNode currentMergeNode =
                            currentMergeNodes.FirstOrDefault(node => node.OriginalValue == property);
                        if (currentMergeNode == null)
                        {
                            Debug.Log("Could not find mergeNode for property: " + property);
                            continue;
                        }

                        properties[i] = currentMergeNode.NameToExportTo;

                        currentMergeNodes =
                            foundScripts
                                .FirstOrDefault(script => script.oldClassModel.FullName == currentMergeNode.Type)
                                ?.MergeNodes;
                    }

                    int line = modification["propertyPath"].Start.Line - 1;
                    scene[line] = scene[line].ReplaceFirst(propertyPath, string.Join(".", properties));
                }
            }
        }


        /// <summary>
        /// Helper method for the<see cref="MigrateFields"/> to replace the fields in the scripts.
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="currentMergeNodes"></param>
        /// <param name="rootYamlNode"></param>
        /// <returns></returns>
        private string[] recursiveReplaceField(ref string[] scene, List<MergeNode> currentMergeNodes,
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
                    scene = handleMappingNode(ref scene, currentMergeNode, foundScripts, yamlNode);
                }
                else if (yamlNode.Value is YamlSequenceNode)
                {
                    scene = handleSequenceNode(ref scene, currentMergeNode, foundScripts, yamlNode);
                }
                else
                {
                    scene = handleValueNode(ref scene, currentMergeNode, yamlNodeKey, line, yamlNode);
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
        private bool CheckCustomLogic(ref string[] scene,
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

        private string[] handleValueNode(ref string[] scene, MergeNode currentMergeNode, string yamlNodeKey,
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

        private string[] handleMappingNode(ref string[] scene, MergeNode currentMergeNode,
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
                scene = recursiveReplaceField(ref scene, typeNodes, yamlNode.Value, foundScripts);
            }
            else
            {
                Debug.Log("Found a mappingNode but could not find subclasses in class : " + type);
            }

            return scene;
        }

        private string[] handleSequenceNode(ref string[] scene, MergeNode currentMergeNode, List<FoundScript>
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
                scene = recursiveReplaceField(ref scene, foundScript.MergeNodes, item, foundScripts);
            }

            return scene;
        }
    }
}
#endif