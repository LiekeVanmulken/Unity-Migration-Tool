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
    public class OptionsWizard : ScriptableWizard
    {
        
        public bool completed;
        
        
        private int index;
        private string label;
        private string comparison;
        private string[] options;

        public event EventHandler<string> onComplete;

        
        [MenuItem("WizardTest/Wizard")]
        public static OptionsWizard CreateWizard()
        {
            return CreateWizard("testlabel", "u040.OriginalClass", new[] {"u040.OriginalClass2", "u040.reflection.OriginalClass", "u040.OriginalField"});
        }

        public static OptionsWizard CreateWizard(string label, string comparison, string[] options)
        {
            var wizard = DisplayWizard<OptionsWizard>("Choose", "Choose");
            wizard.label = label;
            wizard.comparison = comparison;
            wizard.options = options;
            return wizard;
        }

        protected override bool DrawWizardGUI()
        {
            GUILayout.Label(label);
            GUILayout.Space(10);
            
            GUILayout.BeginHorizontal();
            
            GUILayout.BeginVertical();
            GUILayout.Label("Original");
            GUILayout.Label(comparison);
            GUILayout.EndVertical();
            
            GUILayout.BeginVertical();
            GUILayout.Space(10);
            GUILayout.Label("Options");            
            index = EditorGUILayout.Popup(index, options);
            GUILayout.EndVertical();
            
            GUILayout.EndHorizontal();
            
            
            return base.DrawWizardGUI();
        }


        void OnWizardCreate()
        {
            completed = true;
            onComplete(this,options[index]);
        }
    }
}
#endif