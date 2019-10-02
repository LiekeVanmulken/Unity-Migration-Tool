using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace importerexporter.models
{
    [Serializable]
    public class MergeNode
    {
        [SerializeField] public string YamlKey;
        [SerializeField] public string NameToExportTo;
        [SerializeField] public string SampleValue;
//        [SerializeField] public string Type;
        [JsonProperty("FieldsToMerge")]
        [SerializeField] public List<MergeNode> MergeNodes;

        public MergeNode()
        {
        }

        public MergeNode(string yamlKey, string nameToExportTo)
        {
            MergeNodes = new List<MergeNode>();
            YamlKey = yamlKey;
            NameToExportTo = nameToExportTo;
        }
    }
}