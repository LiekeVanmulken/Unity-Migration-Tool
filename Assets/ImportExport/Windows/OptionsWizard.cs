#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;


namespace importerexporter.windows
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

//        public event EventHandler<string> onComplete;
        public event Action<string> onComplete;
        public event Action onIgnore;


        [MenuItem("WizardTest/Wizard")]
        public static OptionsWizard CreateWizard()
        {
            return CreateWizard("testlabel", "u040.OriginalClass",
                new[] {"u040.OriginalClass2", "u040.reflection.OriginalClass", "u040.OriginalField"}, result=> { Debug.Log("Select called, result: " + result);},
                () => { Debug.Log("Ignore called");});
        }

        public static OptionsWizard CreateWizard(string label, string comparison, string[] options, Action<string> onComplete, Action onIgnore)
        {
            var wizard = DisplayWizard<OptionsWizard>("Choose", "Choose", "Nothing");
            wizard.label = label;
            wizard.comparison = comparison;
            wizard.options = options;
            wizard.onComplete = onComplete;
            wizard.onIgnore = onIgnore;
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
            onComplete( options[index]);
        }

        private void OnWizardOtherButton()
        {
            completed = true;
            onIgnore();
        }
    }
}
#endif