#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionMethods;
using UnityEditor;
using UnityEngine;
using YamlDotNet.RepresentationModel;
using FoundScript = importerexporter.ImportExportUtility.FoundScript;


namespace importerexporter
{
    public class MergingWizard : ScriptableWizard
    {
        private Constants constants = Constants.Instance;

        private List<FoundScript> foundScripts;

//        private List<MergeNode> mergeNodes;
        public event EventHandler<List<FoundScript>> onComplete;


        public class MergeNode
        {
            public string YamlKey;
            public string ValueToExportTo;
            public List<MergeNode> MergeNodes = new List<MergeNode>();

            public bool IsRoot;
            public FoundScript FoundScript;

            public MergeNode()
            {
            }

            public MergeNode(string yamlKey, string valueToExportTo)
            {
                YamlKey = yamlKey;
                ValueToExportTo = valueToExportTo;
            }
        }


        [MenuItem("WizardTest/Wizard")]
        public static MergingWizard CreateWizard(List<FoundScript> scriptsToMerge)
        {
            var wizard = DisplayWizard<MergingWizard>("Merge variables", "Merge");
            wizard.foundScripts = scriptsToMerge;

//            wizard.mergeNodes = new List<MergeNode>();


            MergeNode root = new MergeNode();
            root.IsRoot = true;

            foreach (FoundScript script in scriptsToMerge)
            {
                List<MergeNode> mergeNodes = wizard.init(script.classData.FieldDatas, script.yamlOptions);
                script.MergeNodes.AddRange(mergeNodes);
            }

            return wizard;
        }

        private List<MergeNode> init(FieldData[] fieldDatas, YamlNode yamlNode)
        {
            List<MergeNode> mergeNodes = new List<MergeNode>();

            IDictionary<YamlNode, YamlNode> AllYamlFields = yamlNode.GetChildren();
            foreach (KeyValuePair<YamlNode, YamlNode> pair in AllYamlFields)
            {
                MergeNode mergeNode = new MergeNode();

                mergeNode.YamlKey = pair.Key.ToString();
                string closest = fieldDatas.OrderBy(field => Levenshtein.Compute(pair.Key.ToString(), field.Name))
                    .First().Name;

                //check if it's one of the default fields that don't really change
                if (constants.MonoBehaviourFieldExclusionList.Contains(mergeNode.YamlKey))
                {
                    closest = "";
                }

                //Set the value that the fields needs to be changed to, to the closest
                mergeNode.ValueToExportTo = closest;

                //Do the same for all the child fields of this node
                if (pair.Value is YamlMappingNode && //Check that it has potentially children 
                    pair.Value.GetChildren().Count > 0 &&
                    !string.IsNullOrEmpty(mergeNode.ValueToExportTo)) //check that it isn't one of the defaults
                {
                    // Get the children of the current field
                    FieldData[] children = fieldDatas.First(data => data.Name == closest).Children;
                    if (children != null)
                    {
                        mergeNode.MergeNodes.AddRange(init(children, pair.Value));
                    }
                }

                mergeNodes.Add(mergeNode);
            }

            return mergeNodes;
        }

        protected override bool DrawWizardGUI()
        {
            if (foundScripts == null || foundScripts.Count == 0)
            {
                Debug.LogError("Init is not working properly");
                return base.DrawWizardGUI();
            }

            for (var i = 0; i < foundScripts.Count; i++)
            {
                FoundScript script = foundScripts[i];
                List<MergeNode> mergeNodes = script.MergeNodes;

                GUILayout.Label("Class : " + script.classData.Name);

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
            if (!mergeNode.IsRoot && !string.IsNullOrEmpty(mergeNode.YamlKey))
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