#if UNITY_EDITOR
using migrationtool.models;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace migrationtool.windows
{
    public class MergeWizard : ScriptableWizard
    {
        public Action<List<ScriptMapping>> onComplete;


        private List<ScriptMapping> scriptMappings;

        private ScriptMappingWrapper[] scriptMappingWrappers;

        GUIStyle richtextStyle;
        GUIStyle classNameStyle;
        GUIStyle paddingStyle;
        GUIStyle horizontalLineStyle;
        GUIStyle verticalMarginStyle;

        Vector2 scrollPosition = Vector2.zero;

        public static MergeWizard CreateWizard(List<ScriptMapping> scriptsToMerge)
        {
            var wizard = DisplayWizard<MergeWizard>("Merge fieldNames", "Merge");
            wizard.scriptMappings = scriptsToMerge;
            wizard.scriptMappingWrappers = new ScriptMappingWrapper[scriptsToMerge.Count];

            for (int i = 0; i < scriptsToMerge.Count; i++)
            {
                wizard.scriptMappingWrappers[i] = new ScriptMappingWrapper(scriptsToMerge[i]);
            }

            return wizard;
        }


        private void OnEnable()
        {
            richtextStyle = new GUIStyle() {richText = true, wordWrap = true};
            
            if (EditorPrefs.GetInt("UserSkin") == 1)
            {
                richtextStyle.normal.textColor = new Color(120, 120, 120);
            }

            classNameStyle = new GUIStyle() {fontSize = 14};
            if (EditorPrefs.GetInt("UserSkin") == 1)
            {
                classNameStyle.normal.textColor = new Color(150, 150, 150);
            }
            
            paddingStyle = new GUIStyle() {padding = new RectOffset(15, 15, 15, 15)};
            horizontalLineStyle = new GUIStyle() {margin = new RectOffset(0, 0, 10, 8), fixedHeight = 1};
            horizontalLineStyle.normal.background = EditorGUIUtility.whiteTexture;
            verticalMarginStyle = new GUIStyle() {margin = new RectOffset(0, 0, 0, 6)};
        }

        private class ScriptMappingWrapper
        {
            public ScriptMapping ScriptMapping;
            public bool[] FieldSelectionStates;
            public int[] OptionSelections;

            public ScriptMappingWrapper(ScriptMapping scriptMapping)
            {
                ScriptMapping = scriptMapping;
                FieldSelectionStates = new bool[scriptMapping.MergeNodes.Count];

                for (var i = 0; i < scriptMapping.MergeNodes.Count; i++)
                {
                    MergeNode mergeNode = scriptMapping.MergeNodes[i];
                    FieldSelectionStates[i] = mergeNode.OriginalValue != mergeNode.NameToExportTo;
                }

                OptionSelections = new int[scriptMapping.MergeNodes.Count];
                for (int i = 0; i < OptionSelections.Length; i++)
                {
                    OptionSelections[i] = 0;
                }
            }
        }

        private GUILayoutOption GetColumnWidth(int _columns)
        {
            float singleColumn = (Screen.width - 50) / 12f;
            return GUILayout.Width(singleColumn * _columns);
        }

        protected override bool DrawWizardGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUIStyle.none, GUI.skin.verticalScrollbar);
            EditorGUILayout.BeginVertical(paddingStyle);

            EditorGUILayout.LabelField(
                "The following class fields differ between the original and current project. Proposals for substitute fields are shown. Please select the correct field manually.",
                richtextStyle);
            GUILayout.Box(GUIContent.none, horizontalLineStyle);

            for (int i = 0; i < scriptMappings.Count; i++)
            {
                ClassModel classModel = scriptMappings[i].newClassModel;
                ScriptMappingWrapper mappingWrapper = scriptMappingWrappers[i];
                List<MergeNode> fieldsToMerge = scriptMappings[i].MergeNodes;

                EditorGUILayout.LabelField(classModel.FullName, classNameStyle);
                GUILayout.Box(GUIContent.none, verticalMarginStyle);

                GUILayout.BeginHorizontal();

                EditorGUILayout.LabelField("<b>Migrate</b>", richtextStyle, GetColumnWidth(1));
                EditorGUILayout.LabelField("<b>Fields</b>", richtextStyle, GetColumnWidth(5));
                EditorGUILayout.LabelField("<b>Type</b>", richtextStyle, GetColumnWidth(6));

                GUILayout.EndHorizontal();

                GUILayout.Box(GUIContent.none, verticalMarginStyle);


                for (int j = 0; j < fieldsToMerge.Count; j++)
                {
                    MergeNode fieldToMerge = fieldsToMerge[j];

                    string originalName = fieldToMerge.OriginalValue;

                    GUILayout.BeginHorizontal();

                    mappingWrapper.FieldSelectionStates[j] =
                        EditorGUILayout.Toggle(mappingWrapper.FieldSelectionStates[j], GetColumnWidth(1));
                    GUI.enabled = mappingWrapper.FieldSelectionStates[j];
                    EditorGUILayout.LabelField(originalName, richtextStyle, GetColumnWidth(5));
                    EditorGUILayout.LabelField(fieldToMerge.Type + (fieldToMerge.IsIterable ? "[]" : ""), richtextStyle,
                        GetColumnWidth(6));

                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();

                    EditorGUILayout.LabelField("", GetColumnWidth(1));

                    EditorGUILayout.BeginVertical();

                    mappingWrapper.OptionSelections[j] = EditorGUILayout.Popup(mappingWrapper.OptionSelections[j],
                        fieldToMerge.Options, GetColumnWidth(5));

                    int optionsIndex = mappingWrapper.OptionSelections[j];
                    if (fieldToMerge.Options != null && optionsIndex < fieldToMerge.Options.Length)
                    {
                        fieldToMerge.NameToExportTo = fieldToMerge.Options[optionsIndex];
                    }
                    else
                    {
                        mappingWrapper.FieldSelectionStates[j] = false;
                    }

                    EditorGUILayout.EndVertical();

                    GUI.enabled = true;

                    GUILayout.EndHorizontal();

                    GUILayout.Box(GUIContent.none, verticalMarginStyle);

                    scriptMappings[i].MergeNodes[j] = fieldToMerge;
                }

                scriptMappingWrappers[i] = mappingWrapper;

                GUILayout.Box(GUIContent.none, horizontalLineStyle);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();

            return base.DrawWizardGUI();
        }

        void OnWizardCreate()
        {
            foreach (ScriptMapping scriptMapping in scriptMappings)
            {
                scriptMapping.HasBeenMapped = ScriptMapping.MappedState.Approved;
            }
            onComplete(scriptMappings);
        }
    }
}
#endif