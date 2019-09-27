using System.Linq;
#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace importerexporter
{
    /// <summary>
    /// Changes the GUIDS and fileIDS to the new GUIDS and fileIDs.
    /// To use this, open Window/Scene import window.
    /// Set the old project path.
    /// Click the import button and select the scene that you wish to replace the fileID's and GUIDs
    /// It will now generate a copy with changed GUIDs and fileIDs causing unity to load the scene without missing references.
    ///
    /// For a more detailed explanation read the README.MD
    /// </summary>
    [Serializable]
    public class ImportWindow : EditorWindow
    {
        [MenuItem("Window/Scene import window")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(ImportWindow));
        }

        private const string EDITORPREFS_KEY = "ImportExportWindow";

        protected void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;
            string savedOldProjectPath = EditorPrefs.GetString(EDITORPREFS_KEY);
            oldProjectPath = savedOldProjectPath;
        }

        protected void OnDisable()
        {
            EditorPrefs.SetString(EDITORPREFS_KEY, oldProjectPath);
        }


        [SerializeField] private static string oldProjectPath;

        /// <summary>
        /// Position of the scroll for the UI
        /// </summary>
        private Vector2 scrollPosition;

        private static List<FileData> oldFileDatas;
        private static string[] lastSceneExport;
        private static List<ImportExportUtility.FoundScript> foundScripts;
        private static MergingWizard mergingWizard;

        private bool debug = true;

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

                string scenePath = EditorUtility.OpenFilePanel("Scene to import", Application.dataPath, "*");
                if (scenePath.Length != 0)
                {
                    List<FileData> oldIDs = ImportExportUtility.ExportClassData(oldProjectPath);
                    List<FileData> currentIDs =
                        debug ? oldIDs : ImportExportUtility.ExportClassData(Application.dataPath);

                    lastSceneExport =
                        ImportExportUtility.ImportClassDataAndTransformIDsInScene(scenePath, oldIDs, currentIDs);
                    
                    foundScripts = ImportExportUtility.FindFieldsToMigrate(lastSceneExport, currentIDs);

                    var now = DateTime.Now;
                    string newScenePath = scenePath + "_imported_" + now.Hour + "_" + now.Minute + "_" +
                                          now.Second + ".unity";
                    File.WriteAllLines(newScenePath
                        , lastSceneExport);
                    EditorUtility.DisplayDialog("Imported data", "The scene was exported to " + newScenePath, "Ok");
                }
                else
                {
                    Debug.LogWarning("No path was selected");
                }
            }

            if (foundScripts != null && lastSceneExport != null && mergingWizard == null)
            {
                List<ImportExportUtility.FoundScript> scripts =
                    foundScripts.Where(field => !field.HasBeenMapped).GroupBy(field => field.fileData.Name)
                        .Select(group => group.First()).ToList();
                mergingWizard = MergingWizard.CreateWizard(scripts);
            }

            EditorGUI.EndDisabledGroup();
        }
    }
}
#endif