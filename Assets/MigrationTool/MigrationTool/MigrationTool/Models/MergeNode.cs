#if UNITY_EDITOR || UNITY_EDITOR_BETA
using System;
using Newtonsoft.Json;
using UnityEngine;

namespace migrationtool.models
{
    [Serializable]
    public class MergeNode
    {
        [SerializeField] public string OriginalValue;
        [SerializeField] public string NameToExportTo;
        [SerializeField] public string Type;
        [SerializeField] public bool IsIterable;
        
        [JsonIgnore] public string[] Options;

        //
        // public MergeNode()
        // {
        // }
        
//        public MergeNode(string originalValue, string nameToExportTo)
//        {
//            OriginalValue = originalValue;
//            NameToExportTo = nameToExportTo;
//        }
    }
}
#endif