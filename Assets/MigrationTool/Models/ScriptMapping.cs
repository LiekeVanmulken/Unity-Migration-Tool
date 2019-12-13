#if UNITY_EDITOR || UNITY_EDITOR_BETA
using System;
using System.Collections.Generic;
using System.Linq;
using migrationtool.utility;
using migrationtool.windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace migrationtool.models
{
    /// <summary>
    /// Class used to return the scripts that need to be remapped by the user.
    /// When the fieldNames in the yaml do not match the fields in the class.
    /// The reason this is returns is because this needs to be filled in by the user and as such
    /// cannot be done in the importExportUtility as this needs user input.
    ///
    /// Used in the <see cref="ImportExportUtility.GenerateFieldMapping"/> and the <seealso cref="MergeWizard"/>
    /// </summary>
    [Serializable]
    public class ScriptMapping
    {
        public ClassModel newClassModel;
        public ClassModel oldClassModel;

        [JsonProperty("FieldsToMerge")] [SerializeField]
        public List<MergeNode> MergeNodes = new List<MergeNode>();

        [JsonConverter(typeof(StringEnumConverter))]
        public MappedState HasBeenMapped = MappedState.NotChecked;

        public enum MappedState
        {
            NotChecked = 0,
            NotMapped,
            Mapped,
            Approved,
            Ignored
        }

        public ScriptMapping()
        {
        }

//        public ScriptMapping(ClassModel oldClassModel, ClassModel newClassModel)
//        {
//            this.newClassModel = newClassModel;
//            this.oldClassModel = oldClassModel;
////            this.YamlOptions = yamlOptions;
//            this.HasBeenMapped = CheckHasBeenMapped(oldClassModel, newClassModel);
//        }

        public MappedState CheckHasBeenMapped()
        {
            if (oldClassModel == null || newClassModel == null)
            {
                throw new NullReferenceException(
                    "Can't call an empty checkHasBeenMapped without knowing the oldClassData and the newClassData");
            }

            return CheckHasBeenMapped(oldClassModel, newClassModel);
        }

        public void GenerateMappingNode()
        {
            if (oldClassModel == null || newClassModel == null)
            {
                throw new NullReferenceException(
                    "Can't call an empty checkHasBeenMapped without knowing the oldClassData and the newClassData");
            }

            foreach (FieldModel field in oldClassModel.Fields)
            {
                MergeNode mergeNode = new MergeNode()
                {
                    OriginalValue = field.Name
                };

                FieldModel mergeNodeType = oldClassModel.Fields
                    .First(data => data.Name == mergeNode.OriginalValue);

                mergeNode.Type = mergeNodeType.Type?.FullName;
                mergeNode.IsIterable = mergeNodeType.IsIterable;

                mergeNode.Options = newClassModel.Fields?
                                        .OrderBy(newField =>
                                            Levenshtein.Compute(
                                                field.Name,
                                                newField.Name))
                                        .Select(data => data.Name).ToArray() ?? new string[0];


                mergeNode.NameToExportTo = mergeNode.Options.Length > 0 ? mergeNode.Options[0] : "";

                MergeNodes.Add(mergeNode);
            }
        }

        /// <summary>
        /// Checks if the field has a exact match between the yaml and the classField
        /// </summary>
        /// <param name="data"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        public MappedState CheckHasBeenMapped(ClassModel oldClassModel,
            ClassModel newClassModel)
        {
            if (HasBeenMapped == MappedState.NotChecked)
            {
                bool result = checkHasBeenMappedRecursive(oldClassModel, newClassModel);
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
        private bool checkHasBeenMappedRecursive(ClassModel oldClassModel, ClassModel newClassModel)
        {
            if (oldClassModel.Fields.IsNullOrEmpty() != newClassModel.Fields.IsNullOrEmpty())
            {
                return false;
            }

            if (oldClassModel.Fields.IsNullOrEmpty() && newClassModel.Fields.IsNullOrEmpty())
            {
                return true;
            }


            foreach (FieldModel oldFieldData in oldClassModel.Fields)
            {
                FieldModel found = newClassModel.Fields.FirstOrDefault(data => data.Name == oldFieldData.Name);
                if (found == null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
#endif