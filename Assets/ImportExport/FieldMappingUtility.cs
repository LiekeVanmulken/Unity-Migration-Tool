using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using importerexporter.models;
using importerexporter.utility;
using UnityEditor;
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
            EditorUtility.DisplayProgressBar("Field Migration", "Finding fields to migrate.", 0.5f);
            List<FoundScript> generateFieldMapping = GenerateFieldMapping(linesToChange, oldIDs, currentIDs);
            EditorUtility.ClearProgressBar();

            return generateFieldMapping;
        }

        /// <summary>
        /// Helper method to change the fields in the yaml to the corresponding new name
        /// </summary>
        /// <param name="linesToChange"></param>
        /// <param name="oldIDs"></param>
        /// <param name="oldClassData"></param>
        /// <param name="currentIDs"></param>
        /// <returns></returns>
        private List<FoundScript> GenerateFieldMapping(string[] linesToChange, List<ClassData> oldIDs,
            List<ClassData> currentIDs)
        {
            string content = string.Join("\n", linesToChange);

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

                ClassData currentClassData = currentIDs.FirstOrDefault(data => data.Guid == guid && data.FileID == fileID);
                if (currentClassData == null)
                {
                    ClassData scriptNotPorted = oldIDs.FirstOrDefault(data => data.Guid == guid && data.FileID == fileID);
                    throw new NotImplementedException("Could not find the IDs of the class. The class names might not match between projects. Old script : " + scriptNotPorted );
                }

                ClassData oldClassData = oldIDs.First(data => data.Name == currentClassData.Name);

                FoundScript found = new FoundScript(currentIDs,oldClassData: oldClassData, newClassData: currentClassData,yamlOptions: scriptYaml);
                generateFlattenedMergeNodes(currentIDs,);
                if (!found.HasBeenMapped)
                {
                    foundScripts.Add(found);
                }
            }

            return foundScripts;
        }

        private void generateFlattenedMergeNodes(List<ClassData> newIDs, ref List<MergeNode> mergeNodes,
            ClassData oldClassData,
            ClassData newClassData,
            YamlNode yamlNode)
        {
            IDictionary<YamlNode, YamlNode> AllYamlFields = yamlNode.GetChildren();
            foreach (KeyValuePair<YamlNode, YamlNode> pair in AllYamlFields)
            {
                MergeNode mergeNode = new MergeNode();
                mergeNode.MergeNodes = new List<MergeNode>();
                mergeNode.YamlKey = pair.Key.ToString();
                mergeNode.SampleValue = pair.Value.ToString();

                if (newClassData != null && !constants.MonoBehaviourFieldExclusionList.Contains(mergeNode.YamlKey))
                {
                    mergeNode.Type = oldClassData.FieldDatas.First(data => data.Name == mergeNode.YamlKey)?.Type?.Name;
                    mergeNode.Options = newClassData.FieldDatas?.Where(data => data.Type.Name == mergeNode.Type)
                        .Select(data => data.Name).ToArray();

                    mergeNode.NameToExportTo = newClassData.FieldDatas?.Where(data => data.Type.Name == mergeNode.Type)
                        .OrderBy(field => Levenshtein.Compute(pair.Key.ToString(), field.Name)).First().Name;
                }

//todo this doesn't work yet
                //Do the same for all the child fields of this node
                if (pair.Value is YamlMappingNode && //Check that it has potentially children 
                    pair.Value.GetChildren().Count > 0 &&
                    !string.IsNullOrEmpty(mergeNode.NameToExportTo)) //check that it isn't one of the defaults
                {
                    //todo : subclass can't be found as it;s in a different file (private class)
                    ClassData oldMappingNode =
                        oldClassData.FieldDatas.First(data => data.Name == mergeNode.YamlKey).Type;
                    
                    ClassData newMappingNode = newIDs.FirstOrDefault(data => data.Name == oldMappingNode.Name);
//                    ClassData newMappingNode = newClassData.FieldDatas?.Where(data => data.Type.Name == mergeNode.Type)
//                        .OrderBy(field => Levenshtein.Compute(pair.Key.ToString(), field.Name)).FirstOrDefault()?.Type;
                    generateFlattenedMergeNodes(newIDs, ref mergeNodes, oldMappingNode, newMappingNode, pair.Value);
                }

                mergeNodes.Add(mergeNode);
            }

//            return mergeNodes;
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
                    foundScripts.First(node =>
                        node.ClassData.Guid == guid && node.ClassData.FileID == fileID);
                scene = recursiveReplaceField(scene, scriptType.MergeNodes, script);
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
        private string[] recursiveReplaceField(string[] scene, List<MergeNode> currentMergeNodes, YamlNode rootYamlNode)
        {
            IDictionary<YamlNode, YamlNode> yamlChildren = rootYamlNode.GetChildren();
            foreach (KeyValuePair<YamlNode, YamlNode> yamlNode in yamlChildren)
            {
                string yamlNodeKey = (string) yamlNode.Key;
                int line = yamlNode.Key.Start.Line - 1;
                var currentMergeNode = currentMergeNodes.First(node => node.YamlKey == yamlNodeKey);

                if (!string.IsNullOrEmpty(currentMergeNode.NameToExportTo))
                {
                    scene[line] = scene[line].ReplaceFirst(currentMergeNode.YamlKey, currentMergeNode.NameToExportTo);
                }

                if (yamlNode.Value is YamlMappingNode &&
                    !constants.MonoBehaviourFieldExclusionList.Contains((string) yamlNode.Key))
                {
                    var recursiveChildren = yamlNode.Value.GetChildren();
                    if (recursiveChildren == null || recursiveChildren.Count == 0)
                    {
                        continue;
                    }

                    recursiveReplaceField(scene, currentMergeNode.MergeNodes, yamlNode.Value);
                }
            }

            return scene;
        }
    }
}