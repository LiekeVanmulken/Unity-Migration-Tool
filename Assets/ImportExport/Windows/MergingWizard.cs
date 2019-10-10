#if UNITY_EDITOR

using importerexporter.utility;
using importerexporter.models;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;


namespace importerexporter.windows
{
    /// <summary>
    /// 
    /// </summary>
    public class MergingWizard : ScriptableWizard
    {
        private Constants constants = Constants.Instance;

        private List<FoundScript> foundScripts;
        private string cachedFoundScripts;

//        private List<MergeNode> mergeNodes;
        public event EventHandler<List<FoundScript>> onComplete;

        GUIStyle richtextStyle;


        public static MergingWizard CreateWizard(List<FoundScript> scriptsToMerge)
        {
            MergingWizard wizard = DisplayWizard<MergingWizard>("Merge fieldNames", "Merge");
            wizard.foundScripts = scriptsToMerge;

            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            };
            wizard.cachedFoundScripts = JsonConvert.SerializeObject(wizard.foundScripts, Formatting.Indented, settings);
            wizard.cachedFoundScripts = wizard.syntax(wizard.cachedFoundScripts);

            return wizard;
        }

        private void OnEnable()
        {
            richtextStyle = new GUIStyle() {richText = true, wordWrap = true,padding = new RectOffset(10,10,10,10)};
        }

        private string syntax(string scripts)
        {
            string keyPattern = "\".*?\"(?=.*:)";
            scripts = Regex.Replace(scripts, keyPattern, "<color=darkblue>$0</color>");
            
            string valuePattern = "(?<=:.*)\".*?\"";
            return Regex.Replace(scripts, valuePattern, "<color=green>$0</color>");
        }
        private string removeSyntax(string scripts)
        {
            string beginPattern = "<color=[A-z0-9#]*>";
            string endPattern = "</color>";
            string beginRemoved = Regex.Replace(scripts, beginPattern, "");
            string endRemoved = Regex.Replace(beginRemoved, endPattern, "");
            return endRemoved;
        }

        protected override bool DrawWizardGUI()
        {
            EditorGUILayout.LabelField("Set the <color=green>\"ValueToExportTo\"</color> field in any MergeNode to change the name of the field to the new name.",richtextStyle);
            EditorGUILayout.LabelField("THE <color=green>\"ValueToExportTo\"</color> MIGHT NOT BE CORRECT. Make sure that these are correct to change the field to the new value.",richtextStyle);
            EditorGUILayout.LabelField("To completely ignore the field set the <color=green>\"ValueToExportTo\"</color> to an empty string (\"\").",richtextStyle);
            
            EditorGUILayout.Space();
            EditorGUILayout.Separator();
            
            cachedFoundScripts = GUILayout.TextArea(cachedFoundScripts,richtextStyle);


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
                    GUILayout.Label(mergeNode.NameToExportTo);
                }
                else
                {
                    mergeNode.NameToExportTo = GUILayout.TextField(mergeNode.NameToExportTo);
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
            string export = removeSyntax(cachedFoundScripts);
            foundScripts = JsonConvert.DeserializeObject<List<FoundScript>>(export);
            onComplete(this, foundScripts);
            Debug.Log("Create button clicked");
        }
    }
}
#endif