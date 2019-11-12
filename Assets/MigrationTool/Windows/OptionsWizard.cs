#if UNITY_EDITOR

﻿using System.Linq;
using System;
using UnityEditor;
using UnityEngine;


namespace migrationtool.windows
{
    /// <summary>
    /// 
    /// </summary>
    public class OptionsWizard : ScriptableWizard
    {
        [HideInInspector] public volatile bool completed;

        private int index;
        private string label;
        private string comparison;
        private string[] options;
        
        public event Action<string> onComplete;
        public event Action onIgnore;

        GUIStyle richtextStyle;
        GUIStyle paddingStyle;
        GUIStyle horizontalLineStyle;
        GUIStyle popupStyle;

//        [MenuItem("Window/Test OptionsWindow")]
//        public static OptionsWizard CreateWizard()
//        {
//
//            return CreateWizard("Lorem ipsum dolor sit amet.", "u040.OriginalClass",
//                new[] { "u040.OriginalClass2", "u040.reflection.OriginalClass", "u040.OriginalField" }, result => { Debug.Log("Select called, result: " + result); },
//                () => { Debug.Log("Ignore called"); });
//        }

        public static OptionsWizard CreateWizard(string label, string comparison, string[] options, Action<string> onComplete, Action onIgnore)
        {

            var wizard = DisplayWizard<OptionsWizard>("Choose", "Choose"
//                ,"Ignore"   // todo : needs to be implemented in the foundscript so it doesn't keep making popups
                );
            wizard.label = label;
            wizard.comparison = comparison;
            wizard.options = options;
            wizard.filteredOptions = options;
            wizard.onComplete = onComplete;
            wizard.onIgnore = onIgnore;
            return wizard;
        }

        private void OnEnable()
        {
            richtextStyle = new GUIStyle() { richText = true, wordWrap = true };
            paddingStyle = new GUIStyle() { padding = new RectOffset(15, 15, 15, 15) };
            horizontalLineStyle = new GUIStyle() { margin = new RectOffset(0, 0, 10, 8), fixedHeight = 1 };
            horizontalLineStyle.normal.background = EditorGUIUtility.whiteTexture;
        }

        private GUILayoutOption GetColumnWidth(int _width)
        {
            float singleColumn = (Screen.width - 50) / 12;
            return GUILayout.Width(singleColumn * _width);
        }

        private string[] filteredOptions;
        private string filter = "";
        private string newFilter = "";

        protected override bool DrawWizardGUI()
        {
            EditorGUILayout.BeginVertical(paddingStyle);
            EditorGUILayout.LabelField(label, richtextStyle);

            GUILayout.Box(GUIContent.none, horizontalLineStyle);

            EditorGUILayout.LabelField("Filter Options", richtextStyle);

            GUI.SetNextControlName("filter-input");
            newFilter = EditorGUILayout.TextField(filter);
            GUI.FocusControl("filter-input");

            if (!filter.Equals(newFilter))
            {
                RefreshFilter(newFilter);
            }
            filter = newFilter;

            GUILayout.Box(GUIContent.none, horizontalLineStyle);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Original", GetColumnWidth(3));
            EditorGUILayout.LabelField(comparison, GetColumnWidth(9));

            EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Substitute", GetColumnWidth(3));

            if(popupStyle == null)
            {
                popupStyle = GUI.skin.GetStyle("popup");
            }

            index = EditorGUILayout.Popup(index, filteredOptions, popupStyle);

            EditorGUILayout.EndHorizontal();

            GUILayout.Box(GUIContent.none, horizontalLineStyle);

            EditorGUILayout.EndVertical();

            return base.DrawWizardGUI();
        }

        private void RefreshFilter(string changedFilter)
        {
            changedFilter = changedFilter.ToLower();
            filteredOptions = options.Where(s => s.ToLower().Contains(changedFilter)).ToArray();
            if (filteredOptions.Length == 0)
            {
                filteredOptions = new string[] { options[0] };
            }
        }


        void OnWizardCreate()
        {
            completed = true;
            onComplete(filteredOptions[index]);
        }

        private void OnWizardOtherButton()
        {
            completed = true;
            onIgnore();
            Close();
        }
    }
}
#endif