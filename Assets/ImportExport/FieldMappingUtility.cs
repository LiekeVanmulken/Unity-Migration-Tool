#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using importerexporter.models;
using importerexporter.utility;
using importerexporter.windows;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using YamlDotNet.RepresentationModel;

namespace importerexporter
{
    /// <summary>
    /// Maps fields to move values.
    /// This should always be run after the transform as it assumes that the IDs have been set to the current IDs!!!
    /// </summary>
    public class FieldMappingUtility
    {
        #region Singleton

        private static FieldMappingUtility instance = null;

        private static readonly object padlock = new object();

        FieldMappingUtility()
        {
        }

        public static FieldMappingUtility Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new FieldMappingUtility();
                    }

                    return instance;
                }
            }
        }

        #endregion

        private Constants constants = Constants.Instance;

        /// <summary>
        /// Finds all fields that need to be migrated from the yaml
        /// </summary>
        /// <param name="linesToChange"></param>
        /// <param name="oldIDs"></param>
        /// <param name="oldIDs"></param>
        /// <param name="currentIDs"></param>
        /// <returns></returns>
        public List<FoundScript> FindFieldsToMigrate(string[] linesToChange, List<ClassData> oldIDs,
            List<ClassData> currentIDs)
        {
            ImportWindow.DisplayProgressBar("Field Migration", "Finding fields to migrate.", 0.5f);
            List<FoundScript> generateFieldMapping = GenerateFieldMapping(linesToChange, oldIDs, currentIDs);
            ImportWindow.ClearProgressBar();

            return generateFieldMapping;
        }

        /// <summary>
        /// Helper method to change the fields in the yaml to the corresponding new name
        /// </summary>
        /// <param name="linesToSearch"></param>
        /// <param name="oldIDs"></param>
        /// <param name="oldClassData"></param>
        /// <param name="newIDs"></param>
        /// <returns></returns>
        private List<FoundScript> GenerateFieldMapping(string[] linesToSearch, List<ClassData> oldIDs,
            List<ClassData> newIDs)
        {
            string content = string.Join("\n", linesToSearch);

            YamlStream yamlStream = new YamlStream();
            yamlStream.Load(new StringReader(content));

            List<FoundScript> foundScripts = new List<FoundScript>();

            for (var i = 0; i < yamlStream.Documents.Count; i++)
            {
                YamlDocument document = yamlStream.Documents[i];

                //Only change it if it's a MonoBehaviour as unity script won't be as easily broken
                string type = document.GetName();
                if (type != "MonoBehaviour")
                {
                    continue;
                }

                YamlNode scriptYaml = document.RootNode.GetChildren()["MonoBehaviour"];

                string fileID = (string) scriptYaml["m_Script"]["fileID"];
                string guid = (string) scriptYaml["m_Script"]["guid"];

                ClassData newClassData =
                    newIDs.FirstOrDefault(data => data.Guid == guid && data.FileID == fileID);
                if (newClassData == null)
                {
                    ClassData scriptNotPorted =
                        oldIDs.FirstOrDefault(data => data.Guid == guid && data.FileID == fileID);
                    throw new NotImplementedException(
                        "Could not find the IDs of the class. The class names might not match between projects. Old script : " +
                        scriptNotPorted);
                }

                ClassData oldClassData = oldIDs.First(data => data.Name == newClassData.Name); // todo : this will crash when there is a mapping


                FoundScript found = new FoundScript(newIDs, oldClassData, newClassData, scriptYaml);
                if (!found.HasBeenMapped)
                {
                    loopThroughYamlKeysForTypes(scriptYaml, ref foundScripts, oldClassData, newClassData, newIDs);
                }
            }

            return foundScripts;
        }

        private void loopThroughYamlKeysForTypes(YamlNode yamlNode, ref List<FoundScript> foundTypes,
            ClassData oldDocumentClassData, ClassData newDocumentClassData, List<ClassData> allNewTypes)
        {
            FoundScript type = new FoundScript();
            type.OldClassData = oldDocumentClassData;
            type.ClassData = newDocumentClassData;

            IDictionary<YamlNode, YamlNode> fields = yamlNode.GetChildren();
            foreach (KeyValuePair<YamlNode, YamlNode> field in fields)
            {
                //Standard MonoBehaviour field, don't map
                if (constants.MonoBehaviourFieldExclusionList.Contains(field.Key.ToString()))
                {
                    continue;
                }

                //The type is already mapped
                if (foundTypes.FirstOrDefault(script => script.OldClassData.Name == oldDocumentClassData.Name) != null)
                {
                    continue;
                }

                MergeNode mergeNode = new MergeNode();
                mergeNode.MergeNodes = new List<MergeNode>();
                mergeNode.YamlKey = field.Key.ToString();
                mergeNode.SampleValue = field.Value.ToString();

                FieldData mergeNodeType = oldDocumentClassData.Fields
                    .First(data => data.Name == mergeNode.YamlKey);

                mergeNode.Type = mergeNodeType?.Type?.Name;

                mergeNode.Options = newDocumentClassData.Fields?
                    .Where(data => data.Type.Name == mergeNode.Type)
                    .Select(data => data.Name).ToArray();

                mergeNode.NameToExportTo = newDocumentClassData.Fields?
                    .Where(data => data.Type.Name == mergeNode.Type)
                    .OrderByDescending(newField =>
                        Levenshtein.Compute(
                            field.Key.ToString(),
                            newField.Name))
                    .First()
                    .Name;

                type.MergeNodes.Add(mergeNode);
                if (field.Value is YamlMappingNode)
                {
                    try
                    {
                        ClassData oldFieldType = oldDocumentClassData.Fields
                            .First(data => data.Name == field.Key.ToString()).Type;

                        ClassData newFieldType = allNewTypes.FirstOrDefault(data => data.Name == oldFieldType.Name);
                        if (newFieldType == null)
                        {
                            // Search through the classes as it's a subclass
                            newFieldType = FindClassOrSubClass(allNewTypes, oldFieldType.Name);
                        }

                        loopThroughYamlKeysForTypes(field.Value, ref foundTypes, oldFieldType, newFieldType,
                            allNewTypes);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Could not find foundScript type, this shouldn't fail but it probably will'");
                        throw e;
                    }
                }
            }

            foundTypes.Add(type);
        }


        private ClassData FindClassOrSubClass(List<ClassData> allNewTypes, string nameToLookFor)
        {
            ClassData result = null;
            foreach (ClassData classData in allNewTypes)
            {
                result = FindClassOrSubClassRecursively(classData, nameToLookFor);
                if (result != null)
                {
                    return result;
                }
            }

            throw new NotImplementedException(
                "Could not find foundScript type in the FindClassOrSubClass, this shouldn't fail but it probably will");
        }

        private ClassData FindClassOrSubClassRecursively(ClassData current, string nameToLookFor)
        {
            if (current == null)
            {
                return null;
            }

            if (current.Name == nameToLookFor)
            {
                return current;
            }

            if (current.Fields == null)
            {
                return null;
            }

            ClassData result = null;
            foreach (FieldData field in current.Fields)
            {
                result = FindClassOrSubClassRecursively(field.Type, nameToLookFor);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

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
                        node.ClassData.Guid == guid && node.ClassData.FileID == fileID);
                if (scriptType != null)
                {
                    scene = recursiveReplaceField(scene, scriptType.MergeNodes, script, foundScripts);
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
        private string[] recursiveReplaceField(string[] scene, List<MergeNode> currentMergeNodes, YamlNode rootYamlNode,
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
                if (yamlNode.Value is YamlMappingNode)
                {
                    var recursiveChildren = yamlNode.Value.GetChildren();
                    if (recursiveChildren == null || recursiveChildren.Count == 0)
                    {
                        continue;
                    }
                    //todo : the parent of a children doesn't get changed

                    var type = currentMergeNodes.First(node => node.YamlKey == yamlNodeKey).Type;
                    List<MergeNode> typeNodes = foundScripts.First(script => script.ClassData.Name == type).MergeNodes;
                    scene = recursiveReplaceField(scene, typeNodes, yamlNode.Value, foundScripts);
                }

//                else
//                {
                var currentMergeNode = currentMergeNodes.First(node => node.YamlKey == yamlNodeKey);

                if (!string.IsNullOrEmpty(currentMergeNode.NameToExportTo))
                {
                    scene[line] = scene[line]
                        .ReplaceFirst(currentMergeNode.YamlKey, currentMergeNode.NameToExportTo);
                }

//                }
            }

            return scene;
        }
    }
}
#endif