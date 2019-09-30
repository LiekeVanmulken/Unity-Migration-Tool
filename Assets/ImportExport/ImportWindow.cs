using System.Linq;
using static MergingWizard;
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
        private ImportExportUtility importExportUtility = ImportExportUtility.Instance;

        /// <summary>
        /// Position of the scroll for the UI
        /// </summary>
        private Vector2 scrollPosition;

        private static List<ClassData> oldFileDatas;
        private static string[] lastSceneExport;
        private static List<ImportExportUtility.FoundScript> foundScripts;
        private static MergingWizard mergingWizard;
        private Constants constants = Constants.Instance;

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
                    Import(scenePath);
                }
                else
                {
                    Debug.LogWarning("No path was selected");
                }
            }

            EditorGUI.EndDisabledGroup();
        }

        /// <summary>
        /// Make a copy of the scene file and change the GUIDs, fileIDs and if necessary the fields 
        /// </summary>
        /// <param name="scenePath"></param>
        private void Import(string scenePath)
        {
            List<ClassData> oldIDs = importExportUtility.ExportClassData(oldProjectPath);
            List<ClassData> currentIDs =
                constants.Debug ? oldIDs : importExportUtility.ExportClassData(Application.dataPath);

            lastSceneExport =
                importExportUtility.ImportClassDataAndTransformIDs(scenePath, oldIDs, currentIDs);

            foundScripts = importExportUtility.FindFieldsToMigrate(lastSceneExport, currentIDs);


            if (foundScripts.Count > 0)
            {
                List<ImportExportUtility.FoundScript> scripts =
                    foundScripts.Where(field => !field.HasBeenMapped).GroupBy(field => field.classData.Name)
                        .Select(group => group.First()).ToList();
                mergingWizard = CreateWizard(scripts);

                mergingWizard.onComplete += (sender, list) => { MergingWizardCompleted(list, scenePath, lastSceneExport); };
            }
            else
            {
                SaveFile(scenePath,lastSceneExport);
            }
        }

        /// <summary>
        /// Save 
        /// </summary>
        /// <param name="scenePath"></param>
        /// <param name="linesToWrite"></param>
        private void SaveFile(string scenePath, string[] linesToWrite)
        {
            var now = DateTime.Now;
            string newScenePath = scenePath + "_imported_" + now.Hour + "_" + now.Minute + "_" +
                                  now.Second + ".unity";
            File.WriteAllLines(newScenePath
                , linesToWrite);
            EditorUtility.DisplayDialog("Imported data", "The scene was exported to " + newScenePath, "Ok");
        }

        /// <summary>
        /// Change the fields after merging with the merging window
        /// </summary>
        /// <param name="mergeNodes"></param>
        /// <param name="scenePath"></param>
        /// <param name="linesToChange"></param>
        private void MergingWizardCompleted(List<MergeNode> mergeNodes, string scenePath, string[] linesToChange)
        {
            string[] newSceneExport =
                importExportUtility.ReplaceFieldsByFoundScripts(linesToChange, mergeNodes);
            
            Debug.Log(string.Join("\n", newSceneExport));
            
            SaveFile(scenePath, linesToChange);
        }
    }
}
#endif