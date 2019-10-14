

using System.Linq;
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


        [MenuItem("Window/Test OptionsWindow")]
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
            wizard.filteredOptions = options;
            wizard.onComplete = onComplete;
            wizard.onIgnore = onIgnore;
            return wizard;
        }

        private string[] filteredOptions;
        private string filter = "";
        private string newFilter = "";
        protected override bool DrawWizardGUI()
        {
            GUILayout.Label(label);
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Filter Options");
            newFilter = GUILayout.TextField(filter);
            if (!filter.Equals(newFilter))
            {
                RefreshFilter(newFilter);
            }
            filter = newFilter;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Label("Original");
            GUILayout.Label(comparison);
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Space(10);
            GUILayout.Label("Options");
            index = EditorGUILayout.Popup(index, filteredOptions);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();


            return base.DrawWizardGUI();
        }

        private void RefreshFilter(string changedFilter)
        {
            changedFilter = changedFilter.ToLower();
            filteredOptions = options.Where(s => s.ToLower().Contains(changedFilter)).ToArray();
            if (filteredOptions.Length == 0)
            {
                filteredOptions = new string[]{options[0]};
            }
        }


        void OnWizardCreate()
        {
            
            completed = true;
            onComplete( filteredOptions[index]);
        }

        private void OnWizardOtherButton()
        {
            completed = true;
            onIgnore();
        }
    }
}
#endif