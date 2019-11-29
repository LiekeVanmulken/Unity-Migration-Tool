using System.Collections.Generic;
using System.Text.RegularExpressions;
using migrationtool.controllers.customlogic;
using Newtonsoft.Json;

namespace migrationtool.utility
{
    public class Constants : Singleton<Constants>
    {
        /// <summary>
        /// How long the while loop that is waiting for a thread should sleep before checking the isComplete again.
        /// </summary>
        public readonly int THREAD_WAIT_TIME = 300;

        /// <summary>
        /// Sets how deep the fields of every class will be parsed.
        /// </summary>
        public readonly int RECURSION_DEPTH = 6;

        /// <summary>
        /// Checks whether something is a list or array and parses the containing class
        /// </summary>
        public readonly Regex IsListOrArrayRegex =
            new Regex("(.*?(?=\\[\\]))|((?<=\\[\\[).*?(?=,))", RegexOptions.Compiled);

        /// <summary>
        /// Regex to extract the guid from a line of text
        /// </summary>
        public readonly Regex RegexGuid = new Regex(@"(?<=guid: )[A-z0-9]*", RegexOptions.Compiled);

        /// <summary>
        /// Path to the ProjectIDs.json from the Asset path
        /// </summary>
        public readonly string RelativeExportPath = "/MigrationTool/Exports/ProjectIDs.json";

        /// <summary>
        /// Path to the GeneratedMappings.json from the Asset path
        /// </summary>
        public readonly string RelativeScriptMappingPath = "/MigrationTool/Exports/GeneratedMappings.json";


        /// <summary>
        ///  Sets the formatting of the saved files
        /// </summary>
        public readonly Formatting IndentJson = Formatting.None;

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
//                {typeof(TestScri
//
// ptQuaternion).FullName, new QuaternionCustomMappingLogic()}
            };
    }
}