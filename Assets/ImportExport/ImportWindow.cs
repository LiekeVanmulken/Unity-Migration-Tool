using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace ImportExporter
{
    /// <summary>
    /// Change the GUIDS and fileIDS to the new GUIDS and fileIDs
    /// For a proper explanation <see cref="OldProjectExportWindow"/>
    /// </summary>
    [Serializable]
    public class ImportWindow : EditorWindow
    {
        [MenuItem("Window/Scene import window")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(ImportWindow));
        }

        [SerializeField] private static string oldProjectPath;

        /// <summary>
        /// Position of the scroll for the UI
        /// </summary>
        private Vector2 scrollPosition;

        void OnGUI()
        {
            GUILayout.Label("Old Assets folder : " + oldProjectPath);
            if (GUILayout.Button("Set old project path"))
            {
                string path = EditorUtility.OpenFolderPanel("title", Application.dataPath, "");
                if (path.Length != 0)
                {
                    oldProjectPath = path;
                }
            }

            EditorGUI.BeginDisabledGroup(String.IsNullOrEmpty(oldProjectPath));
            if (GUILayout.Button("Import"))
            {
                if (string.IsNullOrEmpty(oldProjectPath))
                {
                    if (EditorUtility.DisplayDialog("New Project import window",
                        "Please select the path of the old project before proceeding.",
                        "Ok"))
                    {
                        return;
                    }
                }

                List<FileData> oldFileDatas = ImportExportUtility.Export(oldProjectPath);
                string path = EditorUtility.OpenFilePanel("title", Application.dataPath, "*");
                if (path.Length != 0)
                {
                    string[] newScene = ImportExportUtility.Import(path, oldFileDatas);

                    var now = DateTime.Now;
                    string newScenePath = path + "_imported_" + now.Hour + "_" + now.Minute + "_" +
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

            EditorGUI.EndDisabledGroup();
        }
    }
}