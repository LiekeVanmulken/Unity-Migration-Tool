using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExtensionMethods;
using importerexporter;
using UnityEditor;
using UnityEngine;
using YamlDotNet.RepresentationModel;

public class MergingWizard : ScriptableWizard
{
    [MenuItem("WizardTest/Wizard")]
    public static MergingWizard CreateWizard(List<ImportExportUtility.FoundScript> mergeVariables)
    {
        var wizard = ScriptableWizard.DisplayWizard<MergingWizard>("Create Light", "Create");

        wizard.foundScripts = mergeVariables;

        return wizard;
    }


    private List<ImportExportUtility.FoundScript> foundScripts;

    public bool done = false;

    protected override bool DrawWizardGUI()
    {
        if (foundScripts.Count == 0)
        {
            Debug.Log("Found no fields to change");
            return base.DrawWizardGUI();
        }

        for (var i = 0; i < foundScripts.Count; i++)
        {
            GUILayout.Label("class : " + foundScripts[0].fileData.Name);
            ImportExportUtility.FoundScript script = foundScripts[i];

            for (var j = 0; j < script.fileData.FieldDatas.Length; i++)
            {
                var field = script.fileData.FieldDatas[j];
                IDictionary<YamlNode, YamlNode> yamlFields = script.yamlOptions.GetChildren();
                string closest = script.yamlOptions.GetChildren()
                    .Select(pair => pair.Key.ToString()).ToList()
                    .OrderBy(ymlField => Levenshtein.Compute(field.Name, ymlField))
                    .First();

                GUILayout.BeginHorizontal();


                GUILayout.TextField(field.Name);
                GUILayout.TextField(closest);

                GUILayout.EndHorizontal();
            }
        }

        return base.DrawWizardGUI();
    }

    void OnWizardCreate()
    {
        done = true;
        Debug.Log("Create button clicked");
    }
}