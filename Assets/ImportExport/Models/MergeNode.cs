using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace importerexporter.models
{
    [Serializable]
    public class MergeNode
    {
        [SerializeField] public string OriginalValue;
        [SerializeField] public string NameToExportTo;
        [SerializeField] public string SampleValue;
        [SerializeField] public string Type;
        [SerializeField] public string[] Options;

        [JsonProperty("FieldsToMerge")]
        [SerializeField] public List<MergeNode> MergeNodes;

        public MergeNode()
        {
        }

        public MergeNode(string originalValue, string nameToExportTo)
        {
            MergeNodes = new List<MergeNode>();
            OriginalValue = originalValue;
            NameToExportTo = nameToExportTo;
        }
    }
}