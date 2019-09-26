using System.Collections;
using System.Collections.Generic;
using importerexporter;
using UnityEditor;
using UnityEngine;

public class MergingWizard : ScriptableWizard
{
    [MenuItem("WizardTest/Wizard")]
    public static MergingWizard CreateWizard(List<ImportExportUtility.FoundField> mergeVariables)
    {
        
        var wizard = ScriptableWizard.DisplayWizard<MergingWizard>("Create Light", "Create");
        
        wizard.foundFields = mergeVariables;
        
        return wizard;

    }


    private List<ImportExportUtility.FoundField> foundFields; 
    
    public bool done = false;

    protected override bool DrawWizardGUI()
    {
        
        
        for (var i = 0; i < foundFields.Count; i++)
        {
            ImportExportUtility.FoundField mergeVariable = foundFields[i];

            foreach (var field in foundFields)
            {
                GUILayout.BeginHorizontal();
                
                var key = GUILayout.TextField(mergeVariable.Key);
                var value = GUILayout.TextField(mergeVariable.Value);
                
                foundFields[i] = new KeyValuePair<string, string>(key, value);    
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