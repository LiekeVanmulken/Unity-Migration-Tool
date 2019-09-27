using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionMethods;
using importerexporter;
using UnityEditor;
using UnityEngine;
using YamlDotNet.RepresentationModel;
using FoundScript = importerexporter.ImportExportUtility.FoundScript;

public class MergingWizard : ScriptableWizard
{
    public bool done;
    private List<FoundScript> foundScripts;
    private List<RecursiveObject> recursiveObjects;

    private class RecursiveObject
    {
        public string YamlKey;
        public string Selected;
        public List<RecursiveObject> RecursiveObjects = new List<RecursiveObject>();
        public bool IsRoot;

        public RecursiveObject()
        {
        }

        public RecursiveObject(string yamlKey, string selected)
        {
            YamlKey = yamlKey;
            Selected = selected;
        }
    }


    [MenuItem("WizardTest/Wizard")]
    public static MergingWizard CreateWizard(List<FoundScript> mergeVariables)
    {
        var wizard = DisplayWizard<MergingWizard>("Merge variables", "Merge");
        wizard.foundScripts = mergeVariables;

        wizard.recursiveObjects = new List<RecursiveObject>();
        foreach (FoundScript script in mergeVariables)
        {
            wizard.recursiveObjects.Add(wizard.init(script.fileData.FieldDatas, script.yamlOptions, true));
        }

        return wizard;
    }

    private RecursiveObject init(FieldData[] fieldDatas, YamlNode yamlNode, bool IsRoot = false)
    {
        RecursiveObject root = new RecursiveObject();
        root.IsRoot = IsRoot;

        IDictionary<YamlNode, YamlNode> AllYamlFields = yamlNode.GetChildren();
        foreach (KeyValuePair<YamlNode, YamlNode> pair in AllYamlFields)
        {
            RecursiveObject recursiveObject = new RecursiveObject();

            recursiveObject.YamlKey = pair.Key.ToString();
            string closest = fieldDatas.OrderBy(field => Levenshtein.Compute(pair.Key.ToString(), field.Name))
                .First().Name;
            if (closest.StartsWith("_m"))
            {
                closest = "";
            }

            recursiveObject.Selected = closest;
            if (pair.Value is YamlMappingNode && pair.Value.GetChildren().Count>0)
            {
//                IDictionary<YamlNode, YamlNode> dictionary = pair.Key.GetChildren();
//                List<RecursiveObject> children = new List<RecursiveObject>();
//                foreach (KeyValuePair<YamlNode, YamlNode> child in dictionary[closest].GetChildren())
//                {
                    recursiveObject.RecursiveObjects.Add(
                        init(
                            fieldDatas.First(data => data.Name == closest).Children,                    //todo : crashes unity
                            pair.Value));
//                }

//                recursiveObject.RecursiveObjects = children;
            }

            root.RecursiveObjects.Add(recursiveObject);
        }

        return root;
    }

    protected override bool DrawWizardGUI()
    {
        if (recursiveObjects.Count == 0 || recursiveObjects.Count != foundScripts.Count)
        {
            Debug.LogError("Init is not working properly");
            return base.DrawWizardGUI();
        }

        for (var i = 0; i < recursiveObjects.Count; i++)
        {
            FoundScript script = foundScripts[i];
            RecursiveObject recursiveObject = recursiveObjects[i];

            GUILayout.Label("class : " + script.fileData.Name);
            recursiveOnGUI(recursiveObject);
        }

        return base.DrawWizardGUI();
    }

    private void recursiveOnGUI(RecursiveObject recursiveObject)
    {
        if (!recursiveObject.IsRoot)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(recursiveObject.YamlKey);
            if (recursiveObject.YamlKey.StartsWith("m_"))
            {
                GUILayout.Label(recursiveObject.Selected);
            }
            else
            {
                recursiveObject.Selected = GUILayout.TextField(recursiveObject.Selected);
            }

            GUILayout.EndHorizontal();
        }

        if (recursiveObject.RecursiveObjects != null && recursiveObject.RecursiveObjects.Count > 0)
        {
            EditorGUI.indentLevel++;
            foreach (RecursiveObject child in recursiveObject.RecursiveObjects)
            {
                recursiveOnGUI(child);
            }

            EditorGUI.indentLevel--;
        }
    }

    void OnWizardCreate()
    {
        done = true;
        Debug.Log("Create button clicked");
    }
}