#if UNITY_EDITOR

using importerexporter.models;
using importerexporter.utility;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Threading;
using importerexporter.views;
using SceneView = importerexporter.views.SceneView;

namespace importerexporter.windows
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
    public class MigrationWindow : MainThreadDispatcherEditorWindow
    {
        private readonly IDExportView idExportView = new IDExportView();
        private readonly SceneView sceneView = new SceneView();
        private readonly PrefabView prefabView = new PrefabView();

        /// <summary>
        /// Location of the Export.json in the current project
        /// </summary>
        private string idExportPath;
        
        /// <summary>
        /// Cache of if the Export.json exists
        /// </summary>
        bool exportExists;


        private List<ClassModel> cachedLocalIds;


        [MenuItem("Window/Scene migration window")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow<MigrationWindow>("Migration Window");
        }

        protected void OnEnable()
        {
            idExportPath = Application.dataPath + "/ImportExport/Exports/Export.json";
        }

        void OnGUI()
        {
            if (GUILayout.Button("Export Classes of the current project"))
            {
                string rootPath = Application.dataPath;

                new Thread(
                    () => { idExportView.ExportCurrentClassData(rootPath); }
                ).Start();
            }

            exportExists = File.Exists(idExportPath);

            EditorGUI.BeginDisabledGroup(!exportExists);
            if (GUILayout.Button("Migrate scene"))
            {
                sceneView.ImportClassDataAndScene();
            }

            if (GUILayout.Button("Convert prefab"))
            {
                CopyPrefabs();
            }

            EditorGUI.EndDisabledGroup();

            GUILayout.Label(exportExists
                ? "IDs found, ready to migrate."
                : "No IDs found, please export the current IDs.");
        }

        private void CopyPrefabs()
        {
            const string sceneFile =
                @"D:\UnityProjects\GITHUB\ImportingOldTestProject\Assets\Scenes\Prefab scene.unity";

            string originalAssetPath = ProjectPathUtility.getProjectPathFromFile(sceneFile);
            const string destinationAssetPath = @"D:\UnityProjects\GITHUB\SceneImportExporter\Assets\";

            prefabView.ParsePrefab(sceneFile, originalAssetPath, destinationAssetPath);
        }

        #region ThreadedUI

        /// <summary>
        /// Open a options window, to choose between classes from a different thread
        /// </summary>
        /// <param name="label"></param>
        /// <param name="original"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static string OpenOptionsWindow(string label, string original, string[] options)
        {
            string result = null;
            bool completed = false;
            Instance().Enqueue(() =>
            {
                Action<string> onComplete = wizardResult =>
                {
                    result = wizardResult;
                    completed = true;
                };
                Action onIgnore = () => { completed = true; };

//                OptionsWizard optionsWizard =
                    OptionsWizard.CreateWizard(label, original, options, onComplete, onIgnore);
            });

            while (!completed)
            {
                Thread.Sleep(100);
            }

            Debug.Log("OptionsWindow result : " + result);
            return result;
        }

        /// <summary>
        /// Display a dialog from a different thread
        /// </summary>
        /// <param name="title"></param>
        /// <param name="info"></param>
        public static void DisplayDialog(string title, string info)
        {
            Instance().Enqueue(() => { EditorUtility.DisplayDialog(title, info, "Ok"); });
        }

        /// <summary>
        /// Display the progressbar from a different thread
        /// </summary>
        /// <param name="title"></param>
        /// <param name="info"></param>
        /// <param name="info"></param>
        /// <param name="progress"></param>
        public static void DisplayProgressBar(string title, string info, float progress)
        {
            Instance().Enqueue(() => { EditorUtility.DisplayProgressBar(title, info, progress); });
        }

        /// <summary>
        /// Clear the progressbar from a different thread
        /// </summary>
        public static void ClearProgressBar()
        {
            Instance().Enqueue(EditorUtility.ClearProgressBar);
        }

        #endregion
    }
}
#endif