using System.Collections.Generic;
using System.Text.RegularExpressions;
using importerexporter.controllers.customlogic;

namespace importerexporter.utility
{
    public class Constants:Singleton<Constants>
    {
        public readonly bool DEBUG = false;

        /// <summary>
        /// Sets how deep the fields of every class will be parsed.
        /// </summary>
        public readonly int RECURSION_DEPTH = 3;
        
        /// <summary>
        /// Checks whether something is a list or array and parses the containing class
        /// </summary>
        public readonly Regex IsListOrArrayRegex = new Regex("(.*?(?=\\[\\]))|((?<=\\[\\[).*?(?=,))", RegexOptions.Compiled);
        
        /// <summary>
        /// Regex to extract the guid from a line of text
        /// </summary>
        public readonly Regex RegexGuid = new Regex(@"(?<=guid: )[A-z0-9]*", RegexOptions.Compiled);
        
        
        
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

        /// <summary>
        /// Mapping of the custom logic
        /// </summary>
        public readonly Dictionary<string, ICustomMappingLogic> CustomLogicMapping =
            new Dictionary<string, ICustomMappingLogic>()
            {
//                {typeof(TestScriptQuaternion).FullName, new QuaternionCustomMappingLogic()}
            };
    }
}