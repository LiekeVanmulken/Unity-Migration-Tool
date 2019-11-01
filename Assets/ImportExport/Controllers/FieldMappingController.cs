

using YamlDotNet.Serialization;
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
                    CheckCustomLogic(ref scene, scriptType, document);
                }
                else
                {
                    Debug.Log("Script found that has no mapping (No members will be replaced), Node: " +
                              document.RootNode.ToString());
                }
               

            }
//            var serializer = new SerializerBuilder().Build();
            
            
            var serializer = new SerializerBuilder().Build();
            string yaml = serializer.Serialize(yamlStream);

            
            Debug.Log( "test + \n " + yaml);

            return scene;
        }

        private static List<KeyValuePair<Type, CustomMappingLogicAttribute>> getCustomLogics()
        {
            List<KeyValuePair<Type, CustomMappingLogicAttribute>> pairs =
                new List<KeyValuePair<Type, CustomMappingLogicAttribute>>();

            // this is making the assumption that all assemblies we need are already loaded.
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    object[] attribs = type.GetCustomAttributes(typeof(CustomMappingLogicAttribute), false);
                    if (attribs != null && attribs.Length > 0)
                    {
                        pairs.Add(new KeyValuePair<Type, CustomMappingLogicAttribute>(type,
                            (CustomMappingLogicAttribute) attribs.First()));
                    }
                }
            }

            return pairs;
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
//                    continue;
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
            FoundScript foundScript, YamlDocument document)
        {
            if (!Constants.Instance.CustomLogicMapping.ContainsKey(foundScript.newClassModel.FullName))
            {
                return false;
            }
            ICustomMappingLogic customLogic = Constants.Instance.CustomLogicMapping[foundScript.newClassModel.FullName];
            customLogic.CustomLogic(ref document, foundScript);
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