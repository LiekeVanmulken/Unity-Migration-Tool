#if UNITY_EDITOR
using migrationtool.utility;
using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Threading;
using migrationtool.views;
using SceneView = migrationtool.views.SceneView;

namespace migrationtool.windows
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
        private readonly Constants constants = Constants.Instance;

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
        private bool exportExists;

        /// <summary>
        /// UI toggle to show batch processing buttons 
        /// </summary>
        private bool batchProcessingEnabled;

        [MenuItem("Window/Migration Tool")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow<MigrationWindow>("Migration Tool");
        }

        protected void OnEnable()
        {
            idExportPath = Application.dataPath + constants.RelativeExportPath;
        }

        void OnGUI()
        {
            if (GUILayout.Button("Export Classes of the current project"))
            {
                string rootPath = Application.dataPath;
                ThreadUtil.RunThread(() => { idExportView.ExportCurrentClassData(rootPath); });
            }

            exportExists = File.Exists(idExportPath);

            EditorGUI.BeginDisabledGroup(!exportExists);
            if (GUILayout.Button("Migrate  scene  to current project"))
            {
                sceneView.MigrateScene();
            }

            EditorGUI.EndDisabledGroup();
            GUILayout.Label(exportExists
                ? "IDs found, ready to migrate."
                : "No IDs found, please export the current IDs.");

            EditorGUI.BeginDisabledGroup(!exportExists);
            GUILayout.Space(20);

            EditorGUILayout.Separator();
            batchProcessingEnabled = EditorGUILayout.Foldout(batchProcessingEnabled, "Batch processing tools");
            if (!batchProcessingEnabled)
            {
                return;
            }

            if (GUILayout.Button("Migrate all  prefabs  from project folder"))
            {
                string rootPath = Application.dataPath;
                ThreadUtil.RunThread(() => { prefabView.MigrateAllPrefabs(rootPath); });
            }

            if (GUILayout.Button("Migrate all  scenes  from project folder"))
            {
                sceneView.MigrateAllScenes();
            }
            EditorGUI.EndDisabledGroup();
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
                Thread.Sleep(Constants.Instance.THREAD_WAIT_TIME);
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