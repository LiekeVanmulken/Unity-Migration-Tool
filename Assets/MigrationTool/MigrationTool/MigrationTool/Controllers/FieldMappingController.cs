#if UNITY_EDITOR || UNITY_EDITOR_BETA
using System;
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
        /// <param name="scriptMappings"></param>
        /// <param name="oldRootPath"></param>
        /// <param name="destinationPath"></param>
        /// <returns></returns>
        public string[]
            MigrateFields(string scenePath, ref string[] scene, ref List<ScriptMapping> scriptMappings,
                string oldRootPath,
                string destinationPath)
        {
            try
            {
                string sceneContent = string.Join("\n", scene.PrepareSceneForYaml());

                YamlStream yamlStream = new YamlStream();
                StringReader tempStringReader = new StringReader(sceneContent);
                yamlStream.Load(tempStringReader);
                tempStringReader.Close();

                int amountOfPrefabs =
                    yamlStream.Documents.Count(document => document.GetName() == "PrefabInstance");
                if (amountOfPrefabs > 0)
                {
                    if (Administration.Instance.MigrateScenePrefabDependencies)
                    {
                        new PrefabView().MigratePrefabsInAScene(scenePath, oldRootPath, destinationPath,
                            ref scriptMappings);
                    }

                    if (scriptMappings == null)
                    {
                        throw new NullReferenceException("Script mappings were null.");
                    }
                    ConvertPrefabsDataInScene(ref scene, oldRootPath, yamlStream, scriptMappings);
                }

                ConvertScene(scenePath, ref scene, scriptMappings, yamlStream);
                
            }
            catch (Exception e)
            {
                Debug.LogError("Could not map fields for scene: " + scenePath + "\r\nException: " + e);
            }

            return scene;
        }

        private void ConvertScene(string scenePath, ref string[] scene, List<ScriptMapping> scriptMappings,
            YamlStream yamlStream)
        {
            List<YamlDocument> yamlDocuments =
                yamlStream.Documents.Where(document => document.GetName() == "MonoBehaviour").ToList();
            foreach (YamlDocument document in yamlDocuments)
            {
                try
                {
                    YamlNode script = document.RootNode.GetChildren()["MonoBehaviour"]; //todo : duplicate code, fix 

                    string fileID = (string) script["m_Script"]["fileID"];
                    string guid = (string) script["m_Script"]["guid"];

                    ScriptMapping scriptMappingType =
                        scriptMappings.FirstOrDefault(node =>
                            node.newClassModel.Guid == guid && node.newClassModel.FileID == fileID);

                    if (scriptMappingType != null)
                    {
                        if (scriptMappingType.HasBeenMapped == ScriptMapping.MappedState.NotMapped ||
                            scriptMappingType.HasBeenMapped == ScriptMapping.MappedState.Approved)
                        {
                            scene = recursiveReplaceField(ref scene, scriptMappingType.MergeNodes, script,
                                scriptMappings);
                        }

                        CheckCustomLogic(ref scene, document, scriptMappingType);
                    }
                    else
                    {
                        Debug.Log("Script found that has no mapping (No members will be replaced) in scene : " +
                                  scenePath +
                                  ", Node: " +
                                  document.RootNode.ToString());
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Could not convert scene in the FieldMappingController scene: " + scenePath +
                                   "\r\nException:" + e);
                }
            }
        }

        private void ConvertPrefabsDataInScene(ref string[] scene, string oldRootPath, YamlStream yamlStream,
            List<ScriptMapping> scriptMappings)
        {
            // Copy prefabs
            List<YamlDocument> yamlPrefabs =
                yamlStream.Documents.Where(document => document.GetName() == "PrefabInstance").ToList();

            List<PrefabModel> oldPrefabs = prefabController.ExportPrefabs(oldRootPath);
            foreach (YamlDocument prefabInstance in yamlPrefabs)
            {
                //Get the prefab file we're working with
                string prefabGuid = (string) prefabInstance.RootNode["PrefabInstance"]["m_SourcePrefab"]["guid"];
                PrefabModel prefabModel = oldPrefabs.FirstOrDefault(prefabFile => prefabFile.Guid == prefabGuid);
                if (prefabModel == null || string.IsNullOrEmpty(prefabModel.Path))
                {
                    Debug.LogWarning(
                        "Found reference to prefab, but could not find the prefab. Might be a model file, not migrating. Prefab guid: " +
                        prefabGuid);
                    continue;
                }

                //Load in the prefab file
                YamlStream prefabStream = new YamlStream();
                string[] lines = File.ReadAllLines(prefabModel.Path).PrepareSceneForYaml();
                StringReader tempStringReader =new StringReader(string.Join("\r\n", lines));
                prefabStream.Load(tempStringReader);
                tempStringReader.Close();


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
                        prefabStream.Documents.FirstOrDefault(document =>
                            document.RootNode.Anchor == fileID);
                    if (scriptReference == null)
                    {
//                        // handle nested prefab 
//
//                        int FileID_of_nested_PrefabInstance = 0;
//                        int FileID_of_object_in_nested_Prefab = 0;
//                        var a = (FileID_of_nested_PrefabInstance ^ FileID_of_object_in_nested_Prefab) & 0x7fffffffffffffff;


                        Debug.LogError(
                            "Nested prefab detected! Can not migrate fields in the scene. If there are any field name changes these will not be migrated. Could not find reference to script in file! Currently nested prefabs are not supported.  fileID : " +
                            fileID);
                        continue;
                    }

                    if (scriptReference.GetName() != "MonoBehaviour")
                    {
                        continue;
                    }

                    YamlNode IDs = scriptReference.RootNode["MonoBehaviour"]["m_Script"];

                    string scriptGuid = (string) IDs["guid"];
                    string scriptFileID = (string) IDs["fileID"];

                    ScriptMapping scriptMappingType =
                        scriptMappings.FirstOrDefault(node =>
                            node.oldClassModel.Guid == scriptGuid && node.oldClassModel.FileID == scriptFileID);
                    if (scriptMappingType == null)
                    {
//                        Debug.Log("Could not find mapping for guid: " + scriptGuid + " fileID: " + scriptFileID);
                        continue;
                    }

                    string[] properties = propertyPath.Split('.');
                    List<MergeNode> currentMergeNodes = scriptMappingType.MergeNodes;

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
                            scriptMappings
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
            List<ScriptMapping> scriptMappings)
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
                    Debug.LogError("[DataLoss] Could not find mergeNode for key : " + yamlNodeKey);
                    continue;
                }

                if (yamlNode.Value is YamlMappingNode)
                {
                    scene = handleMappingNode(ref scene, currentMergeNode, scriptMappings, yamlNode);
                }
                else if (yamlNode.Value is YamlSequenceNode)
                {
                    scene = handleSequenceNode(ref scene, currentMergeNode, scriptMappings, yamlNode);
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
        /// <param name="document"></param>
        /// <param name="scriptMapping"></param>
        /// <returns>True when it handles logic, else false and it still needs to be handled</returns>
        private bool CheckCustomLogic(ref string[] scene,
            YamlDocument document, ScriptMapping scriptMapping)
        {
            if (!Constants.Instance.CustomLogicMapping.ContainsKey(scriptMapping.newClassModel.FullName))
            {
                return false;
            }

            ICustomMappingLogic customLogic =
                Constants.Instance.CustomLogicMapping[scriptMapping.newClassModel.FullName];
            customLogic.CustomLogic(ref scene, ref document, scriptMapping);
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
            List<ScriptMapping> scriptMappings
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
                scriptMappings.FirstOrDefault(script => script.oldClassModel.FullName == type)?.MergeNodes;
            if (typeNodes != null)
            {
                scene = recursiveReplaceField(ref scene, typeNodes, yamlNode.Value, scriptMappings);
            }
            else
            {
                Debug.Log("Found a mappingNode but could not find subclasses in class : " + type);
            }

            return scene;
        }

        private string[] handleSequenceNode(ref string[] scene, MergeNode currentMergeNode, List<ScriptMapping>
            scriptMappings, KeyValuePair<YamlNode, YamlNode> yamlNode)
        {
            int line = yamlNode.Key.Start.Line - 1;
            scene[line] = scene[line].ReplaceFirst(currentMergeNode.OriginalValue, currentMergeNode.NameToExportTo);
            string type = currentMergeNode.Type;

            ScriptMapping scriptMapping =
                scriptMappings.FirstOrDefault(script => script.oldClassModel.Name == type);
            if (scriptMapping == null)
            {
                Debug.Log("Could not find scriptMapping for MergeNode, Type : " + currentMergeNode.Type +
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
                scene = recursiveReplaceField(ref scene, scriptMapping.MergeNodes, item, scriptMappings);
            }

            return scene;
        }
    }
}
#endif