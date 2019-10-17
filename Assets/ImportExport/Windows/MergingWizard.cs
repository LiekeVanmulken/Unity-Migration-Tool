#if UNITY_EDITOR

using importerexporter.utility;
using importerexporter.models;
using System;
using System.Collections.Generic;
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

        public Action<List<FoundScript>> onComplete;

        GUIStyle richtextStyle;
        GUIStyle paddingStyle;

        [MenuItem("WizardTest/Wizard")]
        public static MergingWizard CreateWizard()
        {
            return CreateWizard(null);
        }

        public static MergingWizard CreateWizard(List<FoundScript> scriptsToMerge)
        {
            MergingWizard wizard = DisplayWizard<MergingWizard>("Merge fieldNames", "Merge");
            wizard.foundScripts = scriptsToMerge;
            return wizard;
        }

        private void OnEnable()
        {
            richtextStyle = new GUIStyle() {richText = true, wordWrap = true, padding = new RectOffset(0, 5, 0, 5)};
            paddingStyle = new GUIStyle() {padding = new RectOffset(10, 10, 10, 10) };
        }

        private GUILayoutOption GetColumnWidth(int _width)
        {
            float singleColumn = Screen.width / 12;
            return GUILayout.Width(singleColumn * _width);
        }

        protected override bool DrawWizardGUI()
        {
            /*
            EditorGUILayout.LabelField("Set the <color=green>\"ValueToExportTo\"</color> field in any MergeNode to change the name of the field to the new name.",richtextStyle);
            EditorGUILayout.LabelField("THE <color=green>\"ValueToExportTo\"</color> MIGHT NOT BE CORRECT. Make sure that these are correct to change the field to the new value.",richtextStyle);
            EditorGUILayout.LabelField("To completely ignore the field set the <color=green>\"ValueToExportTo\"</color> to an empty string (\"\").",richtextStyle);
            
            EditorGUILayout.Space();
            EditorGUILayout.Separator();
            
            cachedFoundScripts = GUILayout.TextArea(cachedFoundScripts,richtextStyle);
            */

            EditorGUILayout.BeginVertical(paddingStyle);

            EditorGUILayout.LabelField("The following class fields differ between the original and current project. Proposals for substitute fields are shown. Please select the correct field manually.", richtextStyle);

            foreach (FoundScript foundScript in foundScripts)
            {
                EditorGUILayout.Space();

                ClassData classData = foundScript.NewClassData;
                List<MergeNode> fieldsToMerge = foundScript.MergeNodes;

                EditorGUILayout.LabelField("Class name: <b>" + classData.Name + "</b>", richtextStyle);

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Original field:", GetColumnWidth(4) );
                EditorGUILayout.LabelField("Value:", GetColumnWidth(3) );
                EditorGUILayout.LabelField("Substitute field with:", GetColumnWidth(3) );
                EditorGUILayout.LabelField("Ignore:", GetColumnWidth(2));
                GUILayout.EndHorizontal();

                for(int i = 0; i < fieldsToMerge.Count; i++)
                {
                    MergeNode fieldToMerge = fieldsToMerge[i];
                    string originalName = fieldToMerge.OriginalValue;
                    string originalValue = fieldToMerge.SampleValue;
                    bool ignoreField = fieldToMerge.NameToExportTo != "";

                    GUILayout.BeginHorizontal();

                    EditorGUILayout.LabelField("<b>" + originalName + "</b> (" + fieldToMerge.Type + ")", richtextStyle, GetColumnWidth(4) );
                    EditorGUILayout.LabelField(originalValue, GetColumnWidth(3) );

                    GUI.enabled = !ignoreField;
                    int optionID = EditorGUILayout.Popup(0, fieldToMerge.Options, GetColumnWidth(3) );
                    GUI.enabled = true;

                    ignoreField = EditorGUILayout.Toggle(ignoreField, GetColumnWidth(2));

                    fieldToMerge.NameToExportTo = ignoreField ? "" : fieldToMerge.Options[optionID];

                    GUILayout.EndHorizontal();
                    fieldsToMerge[i] = fieldToMerge;
                }
            }

            GUILayout.EndVertical();

            return base.DrawWizardGUI();
        }
        void OnWizardCreate()
        {
            onComplete( foundScripts);
            Debug.Log("Create button clicked");
        }
    }
}
#endif