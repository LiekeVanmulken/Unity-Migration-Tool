#if UNITY_EDITOR

using importerexporter.utility;
using importerexporter.models;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;


namespace importerexporter
{
    public class MergingWizard : ScriptableWizard
    {
        private Constants constants = Constants.Instance;

        private List<FoundScript> foundScripts;
        private string cachedFoundScripts;

//        private List<MergeNode> mergeNodes;
        public event EventHandler<List<FoundScript>> onComplete;


        [MenuItem("WizardTest/Wizard")]
        public static MergingWizard CreateWizard(List<FoundScript> scriptsToMerge)
        {
            var wizard = DisplayWizard<MergingWizard>("Merge variables", "Merge");
            wizard.foundScripts = scriptsToMerge;

            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            };
            wizard.cachedFoundScripts = JsonConvert.SerializeObject(wizard.foundScripts, Formatting.Indented, settings);
            wizard.cachedFoundScripts = wizard.syntax(wizard.cachedFoundScripts);

            return wizard;
        }

        private string syntax(string scripts)
        {
            string pattern = "\".*?\"";
            return Regex.Replace(scripts, pattern, "<color=green> $1 $2 </color>");
        }

        protected override bool DrawWizardGUI()
        {
            GUIStyle style = new GUIStyle ();
            style.richText = true;
            cachedFoundScripts = GUILayout.TextArea(cachedFoundScripts,style);


            return base.DrawWizardGUI();


            if (foundScripts == null || foundScripts.Count == 0)
            {
                Debug.LogError("Init is not working properly");
                return base.DrawWizardGUI();
            }

            EditorGUILayout.HelpBox("Fields will be changed to the new value in the textbox.", MessageType.Warning);
            EditorGUILayout.HelpBox("Leaving the field empty will completely ignore the field.", MessageType.Warning);
            for (var i = 0; i < foundScripts.Count; i++)
            {
                FoundScript script = foundScripts[i];
                List<MergeNode> mergeNodes = script.MergeNodes;

                GUILayout.Label("Class : " + script.ClassData.Name);

                GUILayout.Space(10);

                foreach (MergeNode mergeNode in mergeNodes)
                {
                    recursiveOnGUI(mergeNode);
                }

                GUILayout.Space(20);
            }

            return base.DrawWizardGUI();
        }

        private const int indent = 20;

        private void recursiveOnGUI(MergeNode mergeNode)
        {
            if (!string.IsNullOrEmpty(mergeNode.YamlKey))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(mergeNode.YamlKey);
                if (constants.MonoBehaviourFieldExclusionList.Contains(mergeNode.YamlKey))
                {
                    GUILayout.Label(mergeNode.ValueToExportTo);
                }
                else
                {
                    mergeNode.ValueToExportTo = GUILayout.TextField(mergeNode.ValueToExportTo);
                }

                GUILayout.EndHorizontal();
            }

            if (mergeNode.MergeNodes != null && mergeNode.MergeNodes.Count > 0)
            {
                EditorGUI.indentLevel += indent;
                foreach (MergeNode child in mergeNode.MergeNodes)
                {
                    recursiveOnGUI(child);
                }

                EditorGUI.indentLevel -= indent;
            }
        }

        void OnWizardCreate()
        {
            onComplete(this, foundScripts);
            Debug.Log("Create button clicked");
        }
    }
}
#endif