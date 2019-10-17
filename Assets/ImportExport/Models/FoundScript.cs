using System;
using System.Collections.Generic;
using System.Linq;
using importerexporter.utility;
using importerexporter.windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

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
        public ClassData NewClassData;
        public ClassData OldClassData;

        [JsonIgnore] public YamlNode YamlOptions;

        [JsonProperty("FieldsToMerge")] [SerializeField]
        public List<MergeNode> MergeNodes = new List<MergeNode>();

        [JsonConverter(typeof(StringEnumConverter))]
        public MappedState HasBeenMapped = MappedState.NotChecked;

        public enum MappedState
        {
            NotChecked = 0,
            NotMapped,
            Mapped
        }

        public FoundScript()
        {
        }

        public FoundScript(ClassData oldClassData, ClassData newNewClassData, YamlNode yamlOptions)
        {
            this.NewClassData = newNewClassData;
            this.OldClassData = oldClassData;
            this.YamlOptions = yamlOptions;
            this.HasBeenMapped = CheckHasBeenMapped(oldClassData, newNewClassData);
        }

        public MappedState CheckHasBeenMapped()
        {
            if (OldClassData == null || NewClassData == null)
            {
                throw new NotImplementedException(
                    "Can't call an empty checkHasBeenMapped without knowing the oldClassData and the newClassData");
            }

            return CheckHasBeenMapped(OldClassData, NewClassData);
        }

        public void GenerateMappingNode(List<FoundScript> existingFoundScripts)
        {
            if (OldClassData == null || NewClassData == null)
            {
                throw new NotImplementedException(
                    "Can't call an empty checkHasBeenMapped without knowing the oldClassData and the newClassData");
            }
            
            foreach (FieldData field in OldClassData.Fields)
            {


                MergeNode mergeNode = new MergeNode();
//                mergeNode.MergeNodes = new List<MergeNode>();
                mergeNode.OriginalValue = field.Name;
//            mergeNode.SampleValue = field.Value.ToString();

                FieldData mergeNodeType = OldClassData.Fields
                    .First(data => data.Name == mergeNode.OriginalValue);

                mergeNode.Type = mergeNodeType?.Type?.Name;

                FoundScript mapping =
                    existingFoundScripts.FirstOrDefault(script => script.OldClassData.Name == mergeNode.Type);

                Func<FieldData, bool> typeCheckPredicate;
                if (mapping == null)
                {
                    typeCheckPredicate = data => data.Type.Name == mergeNode.Type;
                }
                else
                {
                    typeCheckPredicate = data => data.Type.Name == mapping.NewClassData.Name;
                }

                mergeNode.Options = NewClassData.Fields?
                    .Where(typeCheckPredicate) // todo this doesn't work anymore because it needs the foundScript for this
                    .OrderBy(newField =>
                        Levenshtein.Compute(
                            field.Name,
                            newField.Name))
                    .Select(data => data.Name).ToArray();

                mergeNode.NameToExportTo = mergeNode.Options?.Length > 0 ? mergeNode.Options[0] : "";

                MergeNodes.Add(mergeNode);
            }
        }

        /// <summary>
        /// Checks if the field has a exact match between the yaml and the classField
        /// </summary>
        /// <param name="data"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        public MappedState CheckHasBeenMapped(ClassData oldClassData,
            ClassData newClassData)
        {
            if (HasBeenMapped == MappedState.NotChecked)
            {
                bool result = checkHasBeenMappedRecursive(oldClassData, newClassData);
                HasBeenMapped = result ? MappedState.Mapped : MappedState.NotMapped;
            }

            return HasBeenMapped;
        }

        /// <summary>
        /// Checks if the field has a exact match between the yaml and the classField
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool checkHasBeenMappedRecursive(ClassData oldClassData, ClassData newClassData)
        {
            if (oldClassData.Fields.IsNullOrEmpty() != newClassData.Fields.IsNullOrEmpty())
            {
                return false;
            }

            if (oldClassData.Fields.IsNullOrEmpty() && newClassData.Fields.IsNullOrEmpty())
            {
                return true;
            }


            foreach (FieldData oldFieldData in oldClassData.Fields)
            {
                FieldData found = newClassData.Fields.FirstOrDefault(data => data.Name == oldFieldData.Name);
                if (found == null)
                {
                    return false;
                }

                if (found.Type?.Fields != null && !checkHasBeenMappedRecursive(oldFieldData.Type, found.Type))
                {
                    return false;
                }
            }

            return true;
        }
    }
}