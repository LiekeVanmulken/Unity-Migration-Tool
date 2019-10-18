#if UNITY_EDITOR

using importerexporter.utility;
using importerexporter.models;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using System.Linq;

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

        private FoundScriptWrapper[] foundScriptWrappers;

        public event EventHandler<List<FoundScript>> onComplete;

        GUIStyle richtextStyle;
        GUIStyle classNameStyle;
        GUIStyle paddingStyle;
        GUIStyle horizontalLineStyle;
        GUIStyle verticalMarginStyle;

        Vector2 scrollPosition = Vector2.zero;

        [MenuItem("WizardTest/Wizard")]
        public static MergingWizard CreateWizard()
        {
            return CreateWizard(null);
        }

        public static MergingWizard CreateWizard(List<FoundScript> scriptsToMerge)
        {
            //GUI.skin.label.wordWrap = true;

            var wizard = DisplayWizard<MergingWizard>("Merge fieldNames", "Merge");
            wizard.foundScripts = scriptsToMerge;
            wizard.foundScriptWrappers = new FoundScriptWrapper[scriptsToMerge.Count];

            for (int i = 0; i < scriptsToMerge.Count; i++)
            {
                wizard.foundScriptWrappers[i] = new FoundScriptWrapper(scriptsToMerge[i]);
            }

            var settings = new JsonSerializerSettings
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
            richtextStyle = new GUIStyle() { richText = true, wordWrap = true };
            classNameStyle = new GUIStyle() { fontSize = 14 };
            paddingStyle = new GUIStyle() { padding = new RectOffset(15, 15, 15, 15) };
            horizontalLineStyle = new GUIStyle() { margin = new RectOffset(0, 0, 10, 8), fixedHeight = 1 };
            horizontalLineStyle.normal.background = EditorGUIUtility.whiteTexture;
            verticalMarginStyle = new GUIStyle() { margin = new RectOffset(0, 0, 0, 6) };
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

        public class FoundScriptWrapper
        {
            public FoundScript FoundScript;
            public bool[] FieldSelectionStates;
            public int[] OptionSelections;

            public FoundScriptWrapper(FoundScript _foundScript)
            {
                FoundScript = _foundScript;
                FieldSelectionStates = new bool[_foundScript.MergeNodes.Count];
                for (int i = 0; i < FieldSelectionStates.Length; i ++)
                {
                    FieldSelectionStates[i] = true;
                }
                OptionSelections = new int[_foundScript.MergeNodes.Count];
                for (int i = 0; i < OptionSelections.Length; i++)
                {
                    OptionSelections[i] = 0;
                }
            }
        }

        private GUILayoutOption GetColumnWidth(int _columns)
        {
            float singleColumn = (Screen.width - 50) / 12;
            return GUILayout.Width(singleColumn * _columns);
        }

        protected override bool DrawWizardGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUIStyle.none, GUI.skin.verticalScrollbar);
            EditorGUILayout.BeginVertical(paddingStyle);

            EditorGUILayout.LabelField("The following class fields differ between the original and current project. Proposals for substitute fields are shown. Please select the correct field manually.", richtextStyle);
            GUILayout.Box(GUIContent.none, horizontalLineStyle);

            for (int i = 0; i < foundScripts.Count; i++)
            {
                ClassData classData = foundScripts[i].ClassData;
                FoundScriptWrapper wrapper = foundScriptWrappers[i];
                List<MergeNode> fieldsToMerge = foundScripts[i].MergeNodes;

                EditorGUILayout.LabelField(classData.Name, classNameStyle);
                GUILayout.Box(GUIContent.none, verticalMarginStyle);

                GUILayout.BeginHorizontal();

                EditorGUILayout.LabelField("<b>Use</b>", richtextStyle, GetColumnWidth(1));
                EditorGUILayout.LabelField("<b>Fields</b>", richtextStyle, GetColumnWidth(5));
                EditorGUILayout.LabelField("<b>Type</b>", richtextStyle, GetColumnWidth(6));

                GUILayout.EndHorizontal();

                GUILayout.Box(GUIContent.none, verticalMarginStyle);


                for (int j = 0; j < fieldsToMerge.Count; j++)
                {
                    MergeNode fieldToMerge = fieldsToMerge[j];
                    string originalName = fieldToMerge.YamlKey;

                    GUILayout.BeginHorizontal();

                    wrapper.FieldSelectionStates[j] = EditorGUILayout.Toggle(wrapper.FieldSelectionStates[j], GetColumnWidth(1));
                    GUI.enabled = wrapper.FieldSelectionStates[j];
                    EditorGUILayout.LabelField(originalName, richtextStyle, GetColumnWidth(5));
                    EditorGUILayout.LabelField(fieldToMerge.Type, richtextStyle, GetColumnWidth(6));

                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();

                    EditorGUILayout.LabelField("", GetColumnWidth(1));

                    EditorGUILayout.BeginVertical();

                    wrapper.OptionSelections[j] = EditorGUILayout.Popup(wrapper.OptionSelections[j], fieldToMerge.Options, GetColumnWidth(5));

                    fieldToMerge.NameToExportTo = fieldToMerge.Options[wrapper.OptionSelections[j]];

                    EditorGUILayout.EndVertical();

                    GUI.enabled = true;

                    GUILayout.EndHorizontal();

                    GUILayout.Box(GUIContent.none, verticalMarginStyle);

                    foundScripts[i].MergeNodes[j] = fieldToMerge;
                }

                foundScriptWrappers[i] = wrapper;

                GUILayout.Box(GUIContent.none, horizontalLineStyle);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();

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
            //string export = removeSyntax(cachedFoundScripts);
            //foundScripts = JsonConvert.DeserializeObject<List<FoundScript>>(export);
            onComplete(this, foundScripts);
            Debug.Log("Create button clicked");
        }
    }
}
#endif