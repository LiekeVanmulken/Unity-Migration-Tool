using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MergingWizard : ScriptableWizard
{
    [MenuItem("WizardTest/Wizard")]
    public static MergingWizard CreateWizard(KeyValuePair<string,string>[] mergeVariables)
    {
        
        var wizard = ScriptableWizard.DisplayWizard<MergingWizard>("Create Light", "Create");
        wizard.mergeVariables = mergeVariables;
        return wizard;

        //If you don't want to use the secondary button simply leave it out:
        //ScriptableWizard.DisplayWizard<WizardCreateLight>("Create Light", "Create");
    }


    private KeyValuePair<string, string>[] mergeVariables;
    public bool done = false;

    protected override bool DrawWizardGUI()
    {
        for (var i = 0; i < mergeVariables.Length; i++)
        {
            KeyValuePair<string, string> mergeVariable = mergeVariables[i];
            GUILayout.BeginHorizontal();
            var key = GUILayout.TextField(mergeVariable.Key);
            var value = GUILayout.TextField(mergeVariable.Value);
            mergeVariables[i] = new KeyValuePair<string, string>(key, value);

            GUILayout.EndHorizontal();
        }

        return base.DrawWizardGUI();
    }

    void OnWizardCreate()
    {
        done = true;
        Debug.Log("Create button clicked");
    }
}