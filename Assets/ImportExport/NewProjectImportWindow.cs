using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

/// <summary>
/// Change the GUIDS and fileIDS to the new GUIDS and fileIDs
/// For a proper explanation <see cref="OldProjectExportWindow"/>
/// </summary>
public class NewProjectImportWindow : EditorWindow
{
    private static List<FileData> filedata = new List<FileData>();

    [MenuItem("ImportExport/New project import window")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(NewProjectImportWindow));
    }

    /// <summary>
    /// Backing field for the textArea where the json is imported from 
    /// </summary>
    private static string jsonTextArea = "";

    /// <summary>
    /// Position of the scroll for the UI
    /// </summary>
    private Vector2 scrollPosition;

    void OnGUI()
    {
        if (GUILayout.Button("Import"))
        {
            if (string.IsNullOrEmpty(jsonTextArea))
            {
                if (EditorUtility.DisplayDialog("New Project import window",
                    "Please copy the json from the Old project export window in the textarea before proceeding.",
                    "Ok"))
                {
                    return;
                }
            }

            string path = EditorUtility.OpenFilePanel("title", Application.dataPath, "*");
            if (path.Length != 0)
            {
                var content = JsonConvert.DeserializeObject<List<FileData>>(jsonTextArea);
                string[] newScene = ImportExportUtility.Import(path, content);

                var now = DateTime.Now;
                string newScenePath = path + "_imported_" + now.Hour + "_" + now.Minute + "_" + now.Minute + "_" +
                                      now.Second + ".unity";
                File.WriteAllLines(newScenePath
                    , newScene);
                EditorUtility.DisplayDialog("Imported data", "The scene was exported to " + newScenePath, "Ok");
            }
            else
            {
                throw new NotImplementedException("Could not get file");
            }
        }


        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        jsonTextArea = EditorGUILayout.TextArea(jsonTextArea);
        EditorGUILayout.EndScrollView();
    }
}