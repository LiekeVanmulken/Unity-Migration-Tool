using System;
using System.Collections.Generic;
using System.Linq;
using importerexporter.utility;
using Newtonsoft.Json;
using UnityEngine;
using YamlDotNet.RepresentationModel;

namespace importerexporter.models
{
    /// <summary>
    /// Class used to return the scripts that need to be remapped by the user.
    /// When the fieldnames in the yaml do not match the fields in the class.
    /// The reason this is returns is because this needs to be filled in by the user and as such
    /// cannot be done in the importExportUtility as this needs user input.
    ///
    /// Used in the <see cref="ImportExportUtility.GenerateFieldMapping"/> and the <seealso cref="MergingWizard"/>
    /// </summary>
    [Serializable]
    public class FoundScript
    {
        Constants constants = Constants.Instance;

        public ClassData ClassData;
        public ClassData OldClassData;
        [JsonIgnore] public YamlNode YamlOptions;
        [JsonIgnore] public bool HasBeenMapped;

        [JsonProperty("FieldsToMerge")] [SerializeField]
        public List<MergeNode> MergeNodes = new List<MergeNode>();

        public FoundScript()
        {
        }

        public FoundScript(ClassData oldClassData, ClassData newClassData, YamlNode yamlOptions)
        {
            this.ClassData = newClassData;
            this.OldClassData = oldClassData; // todo : implement the generateMergeNodesRecursively 
            this.YamlOptions = yamlOptions;
            this.HasBeenMapped = checkHasBeenMapped(newClassData.FieldDatas, yamlOptions);

            if (!this.HasBeenMapped)
            {
                List<MergeNode> mergeNodes = new List<MergeNode>();
                generateFlattenedMergeNodes(ref mergeNodes, oldClassData, newClassData, this.YamlOptions);
                this.MergeNodes = mergeNodes;
            }
        }

        /// <summary>
        /// Checks if the field has a exact match between the yaml and the classField
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool checkHasBeenMapped(FieldData[] datas, YamlNode node)
        {
            IDictionary<YamlNode, YamlNode> possibilities = node.GetChildren();
            foreach (FieldData fieldData in datas)
            {
                KeyValuePair<YamlNode, YamlNode> found =
                    possibilities.FirstOrDefault(pos =>
                        (string) pos.Key == fieldData.Name);
                if (found.Key == null)
                {
                    return false;
                }

                if (fieldData.Type != null && fieldData.Type.FieldDatas != null &&
                    !checkHasBeenMapped(fieldData.Type.FieldDatas, node[found.Key]))
                {
                    return false;
                }
            }

            return true;
        }

        private void generateFlattenedMergeNodes(ref List<MergeNode> mergeNodes, ClassData oldClassData,
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

////                //Do the same for all the child fields of this node
//                if (pair.Value is YamlMappingNode && //Check that it has potentially children 
//                    pair.Value.GetChildren().Count > 0 &&
//                    !string.IsNullOrEmpty(mergeNode.NameToExportTo)) //check that it isn't one of the defaults
//                {
//                    if (newChildren != null)
//                    {
//                        mergeNode.MergeNodes.AddRange(generateFlattenedMergeNodes(ref mergeNodes, oldChildren,
//                            newChildren,
//                            pair.Value));
//                    }
//                }

                mergeNodes.Add(mergeNode);
            }

//            return mergeNodes;
        }

//
//        private List<MergeNode> GenerateMergeNodesRecursively(FieldData[] oldFieldDatas, FieldData[] newFieldDatas,
//            YamlNode yamlNode)
//        {
//            List<MergeNode> mergeNodes = new List<MergeNode>();
//
//            IDictionary<YamlNode, YamlNode> AllYamlFields = yamlNode.GetChildren();
//            foreach (KeyValuePair<YamlNode, YamlNode> pair in AllYamlFields)
//            {
//                MergeNode mergeNode = new MergeNode();
//                mergeNode.MergeNodes = new List<MergeNode>();
//                mergeNode.YamlKey = pair.Key.ToString();
//                mergeNode.SampleValue = pair.Value.ToString();
//
//                FieldData[] closestFieldDatas = newFieldDatas
//                    .OrderBy(field => Levenshtein.Compute(pair.Key.ToString(), field.Name)).ToArray();
//
//                string closest = closestFieldDatas.First().Name;
//
//                mergeNode.Type = oldFieldDatas.First(data => data.Name == mergeNode.YamlKey).ToString();
//                mergeNode.Options = newFieldDatas.Where(data => data.Type == mergeNode.Type).Select(data => data.Name)
//                    .ToArray();
//
//
//                //check if it's one of the default fields that don't really change
//                if (constants.MonoBehaviourFieldExclusionList.Contains(mergeNode.YamlKey))
//                {
//                    closest = "";
//                }
//
//                //Set the value that the fields needs to be changed to, to the closest
//                mergeNode.NameToExportTo = closest;
//
//                //Do the same for all the child fields of this node
//                if (pair.Value is YamlMappingNode && //Check that it has potentially children 
//                    pair.Value.GetChildren().Count > 0 &&
//                    !string.IsNullOrEmpty(mergeNode.NameToExportTo)) //check that it isn't one of the defaults
//                {
//                    // Get the children of the current field
//                    FieldData[] newChildren = newFieldDatas.First(data => data.Name == closest).Children;
//                    FieldData[] oldChildren = oldFieldDatas.First(data => data.Name == closest).Children;
//                    if (newChildren != null)
//                    {
//                        mergeNode.MergeNodes.AddRange(GenerateMergeNodesRecursively(oldChildren, newChildren,
//                            pair.Value));
//                    }
//                } //todo : look for all classes and make mappings fot them
//
//                mergeNodes.Add(mergeNode);
//            }
//
//            return mergeNodes;
//        }

//        private ClassData[] findClosestMatching(YamlNode yamlNode, List<ClassData> classDatas)
//        {
//            IDictionary<YamlNode, YamlNode> AllYamlFields = yamlNode.GetChildren();
//            KeyValuePair<YamlNode, YamlNode>[] yamlFieldsArray = AllYamlFields.ToArray();
//
//            foreach (ClassData classData in classDatas)
//            {
//                classData.FieldDatas[0]
//
//                foreach (KeyValuePair<YamlNode, YamlNode> pair in AllYamlFields)
//                {
//                    string Key = pair.Key.ToString();
//                    YamlNode value = pair.Value;
//
//                    foreach (ClassData classData in classDatas)
//                    {
//                        classData
//                    }
//                }
//            }
//        }
    }
}