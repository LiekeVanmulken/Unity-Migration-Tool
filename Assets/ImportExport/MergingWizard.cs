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
        private List<MergeNode> mergeNodes;
        public event EventHandler<List<MergeNode>> onComplete;


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
        public static MergingWizard CreateWizard(List<FoundScript> mergeVariables)
        {
            var wizard = DisplayWizard<MergingWizard>("Merge variables", "Merge");
            wizard.foundScripts = mergeVariables;

            wizard.mergeNodes = new List<MergeNode>();
            foreach (FoundScript script in mergeVariables)
            {
                MergeNode mergeNode = wizard.init(script.classData.FieldDatas, script.yamlOptions, true);
                mergeNode.FoundScript = script;
                wizard.mergeNodes.Add(mergeNode);
            }

            return wizard;
        }

        private MergeNode init(FieldData[] fieldDatas, YamlNode yamlNode, bool IsRoot = false)
        {
            MergeNode root = new MergeNode();
            if (IsRoot)
            {
                root.IsRoot = IsRoot;
            }

            IDictionary<YamlNode, YamlNode> AllYamlFields = yamlNode.GetChildren();
            foreach (KeyValuePair<YamlNode, YamlNode> pair in AllYamlFields)
            {
                MergeNode mergeNode = new MergeNode();

                mergeNode.YamlKey = pair.Key.ToString();
                string closest = fieldDatas.OrderBy(field => Levenshtein.Compute(pair.Key.ToString(), field.Name))
                    .First().Name;
                if (constants.MonoBehaviourFieldExclusionList.Contains(mergeNode.YamlKey))
                {
                    closest = "";
                }

                mergeNode.ValueToExportTo = closest;
                if (pair.Value is YamlMappingNode && pair.Value.GetChildren().Count > 0 &&
                    !constants.MonoBehaviourFieldExclusionList.Contains(mergeNode.YamlKey))
                {
                    FieldData[] children = fieldDatas.First(data => data.Name == closest).Children;
                    if (children != null)
                    {
                        mergeNode.MergeNodes.Add(init(children, pair.Value));
                    }
                }

                root.MergeNodes.Add(mergeNode);
            }

            return root;
        }

        protected override bool DrawWizardGUI()
        {
            if (mergeNodes.Count == 0 || mergeNodes.Count != foundScripts.Count)
            {
                Debug.LogError("Init is not working properly");
                return base.DrawWizardGUI();
            }

            for (var i = 0; i < mergeNodes.Count; i++)
            {
                FoundScript script = foundScripts[i];
                MergeNode mergeNode = mergeNodes[i];

                GUILayout.Label("Class : " + script.classData.Name);

                GUILayout.Space(10);
                recursiveOnGUI(mergeNode);
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
            //Remove the empty values
            List<MergeNode> nodesToRemove =
                mergeNodes.Where(mergeNode => string.IsNullOrEmpty(mergeNode.ValueToExportTo) && !mergeNode.IsRoot)
                    .ToList();
            nodesToRemove.ForEach(node => mergeNodes.Remove(node));

            onComplete(this, mergeNodes);
            Debug.Log("Create button clicked");
        }
    }
}
#endif