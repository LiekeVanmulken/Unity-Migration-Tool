using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionMethods;
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
        [JsonIgnore] public YamlNode YamlOptions;
        [JsonIgnore] public bool HasBeenMapped;

        [JsonProperty("FieldsToMerge")] [SerializeField]
        public List<MergeNode> MergeNodes = new List<MergeNode>();

        public FoundScript()
        {
        }

        public FoundScript(ClassData newclassData, YamlNode yamlOptions)
        {
            this.ClassData = newclassData;
            this.YamlOptions = yamlOptions;
            this.HasBeenMapped = checkHasBeenMapped(newclassData.FieldDatas, yamlOptions);

            if (!this.HasBeenMapped)
            {
                MergeNodes = GenerateMergeNodesRecursively(newclassData.FieldDatas, this.YamlOptions);
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

                if (fieldData.Children != null && !checkHasBeenMapped(fieldData.Children, node[found.Key]))
                {
                    return false;
                }
            }

            return true;
        }

        private List<MergeNode> GenerateMergeNodesRecursively(FieldData[] fieldDatas, YamlNode yamlNode)
        {
            List<MergeNode> mergeNodes = new List<MergeNode>();

            IDictionary<YamlNode, YamlNode> AllYamlFields = yamlNode.GetChildren();
            foreach (KeyValuePair<YamlNode, YamlNode> pair in AllYamlFields)
            {
                MergeNode mergeNode = new MergeNode();

                mergeNode.MergeNodes = new List<MergeNode>();
                mergeNode.YamlKey = pair.Key.ToString();
                mergeNode.SampleValue = pair.Value.ToString();

                FieldData closestFieldData = fieldDatas
                    .OrderBy(field => Levenshtein.Compute(pair.Key.ToString(), field.Name))
                    .First();
                string closest = closestFieldData.Name;

                //check if it's one of the default fields that don't really change
                if (constants.MonoBehaviourFieldExclusionList.Contains(mergeNode.YamlKey))
                {
                    closest = "";
                }

                //Set the value that the fields needs to be changed to, to the closest
                mergeNode.NameToExportTo = closest;

                //Do the same for all the child fields of this node
                if (pair.Value is YamlMappingNode && //Check that it has potentially children 
                    pair.Value.GetChildren().Count > 0 &&
                    !string.IsNullOrEmpty(mergeNode.NameToExportTo)) //check that it isn't one of the defaults
                {
                    // Get the children of the current field
                    FieldData[] children = fieldDatas.First(data => data.Name == closest).Children;
                    if (children != null)
                    {
                        mergeNode.MergeNodes.AddRange(GenerateMergeNodesRecursively(children, pair.Value));
                    }
                }

                mergeNodes.Add(mergeNode);
            }

            return mergeNodes;
        }
    }
}