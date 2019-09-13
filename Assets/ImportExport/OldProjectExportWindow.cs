using System;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Window to export the guids and fileID's of a scene.
/// This gets the current scene and loops through all components in the scene.
/// It then matches those components with the data in the unity scene file(yaml).
/// It then saves that data to a string that can be used in the <see cref="NewProjectImportWindow"/> in a different project.
/// After you have exported the data open the new project to which you want to export.
/// And copy the current scene to that project.
/// Then open the <see cref="NewProjectImportWindow"/> in the to export to project and copy the data into the textfield of the <see cref="NewProjectImportWindow"/>.
/// Then press the Import button.
/// This will open up a popup where you can select the scene which to import. Select the scene that you copied and press ok.
/// Then it will make a copy of the scene and change all of the old GUIDS and fileIDs to the new GUIDS and fileIDs.
/// After this the scene should have no missing scripts.  
/// </summary>
public class OldProjectExportWindow : EditorWindow
{
    /// <summary>
    /// Backing field of the textArea and where the json is written to
    /// </summary>
    private string jsonData;
    /// <summary>
    /// Position of the scroll for the ui
    /// </summary>
    private Vector2 scrollPosition;

    [MenuItem("ImportExport/Old project export window")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(OldProjectExportWindow));
    }

    void OnGUI()
    {
        if (GUILayout.Button("Export current scene ID's to json"))
        {
            var rootPath = Application.dataPath.Replace("/Assets", "");
            var path = rootPath + "/" + EditorSceneManager.GetActiveScene().path;

            if (File.Exists(path))
            {
                jsonData = JsonConvert.SerializeObject(ImportExportUtility.Export().ToArray());
                Debug.Log(
                    "Converted the scene yaml to GUIDS json. Please copy the json to the New Project Import Window in the new project.\n Json exported: " +
                    jsonData);
            }
            else
            {
                throw new NotImplementedException("Could not find scene with path : " + path);
            }
        }
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        jsonData = GUILayout.TextArea(jsonData);
        EditorGUILayout.EndScrollView();
    }
}