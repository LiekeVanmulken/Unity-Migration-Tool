using System.Collections.Generic;
using System.Text.RegularExpressions;
using importerexporter.controllers.customlogic;

namespace importerexporter.utility
{
    public class Constants
    {
        #region Singleton

        private static Constants instance = null;

        private static readonly object padlock = new object();


        Constants()
        {
        }

        public static Constants Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new Constants();
                    }

                    return instance;
                }
            }
        }

        #endregion

        public readonly bool DEBUG = false;

        public readonly int RECURSION_DEPTH = 3;
        
        public readonly Regex StandardClassesRegex = new Regex("(UnityEngine|System)\\.[A-z0-9]*");
        public readonly Regex IsListOrArrayRegex = new Regex("(.*?(?=\\[\\]))|((?<=\\[\\[).*?(?=,))");

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
                {typeof(TestScriptQuaternion).FullName, new QuaternionCustomMappingLogic()}
            };
    }
}