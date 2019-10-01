using System;
using System.Collections.Generic;
using UnityEngine;

namespace importerexporter.models
{
    [Serializable]
    public class MergeNode
    {
        [SerializeField] public string YamlKey;
        [SerializeField] public string ValueToExportTo;
//        [SerializeField] public string Type;
        [SerializeField] public List<MergeNode> MergeNodes;

        public MergeNode()
        {
        }

        public MergeNode(string yamlKey, string valueToExportTo)
        {
            MergeNodes = new List<MergeNode>();
            YamlKey = yamlKey;
            ValueToExportTo = valueToExportTo;
        }
    }
}