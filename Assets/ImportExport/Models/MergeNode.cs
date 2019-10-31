using System;
using UnityEngine;

namespace importerexporter.models
{
    [Serializable]
    public class MergeNode
    {
        [SerializeField] public string OriginalValue;
        [SerializeField] public string NameToExportTo;
        [SerializeField] public string Type;
        [SerializeField] public bool IsIterable;
        [SerializeField] public string[] Options;

        public FoundScript Parent;
        
        public MergeNode(FoundScript parent)
        {
            this.Parent = parent;
        }
        
//        public MergeNode(string originalValue, string nameToExportTo)
//        {
//            OriginalValue = originalValue;
//            NameToExportTo = nameToExportTo;
//        }
    }
}