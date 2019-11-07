using System.Collections.Generic;
using System.Text.RegularExpressions;
using importerexporter.controllers.customlogic;

namespace importerexporter.utility
{
    public class Constants:Singleton<Constants>
    {
        public readonly bool DEBUG = false;

        public readonly int RECURSION_DEPTH = 3;
        
        public readonly Regex StandardClassesRegex = new Regex("(UnityEngine|System)\\.[A-z0-9]*",RegexOptions.Compiled);
        public readonly Regex IsListOrArrayRegex = new Regex("(.*?(?=\\[\\]))|((?<=\\[\\[).*?(?=,))", RegexOptions.Compiled);

        /// <summary>
        /// Fields to exclude in the field mapping of the MonoBehaviour Yaml
        /// </summary>
        public readonly List<string> MonoBehaviourFieldExclusionList = new List<string>()
        {
            "m_ObjectHideFlags",
            "m_CorrespondingSourceObject",
            "m_PrefabInstance",
            "m_PrefabAsset",
            "m_GameObject",
            "m_Enabled",
            "m_EditorHideFlags",
            "m_Script",
            "m_Name",
            "m_EditorClassIdentifier"
        };

        public readonly Dictionary<string, ICustomMappingLogic> CustomLogicMapping =
            new Dictionary<string, ICustomMappingLogic>()
            {
//                {typeof(TestScriptQuaternion).FullName, new QuaternionCustomMappingLogic()}
            };
    }
}