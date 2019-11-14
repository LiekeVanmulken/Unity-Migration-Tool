
using Newtonsoft.Json;
#if UNITY_EDITOR
using migrationtool.utility;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Threading;
using migrationtool.models;
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

        /// <summary>
        /// UI toggle to show Advanced options 
        /// </summary>
        private bool advancedOptionsEnabled;

        /// <summary>
        /// Custom path to read the oldIDs from to override the oldIDs
        /// </summary>
        private string customOldIDSPath;

        /// <summary>
        /// UI scroll position
        /// </summary>
        private Vector2 scrollPosition;

        private GUIStyle buttonStyle;

        private GUIStyle labelStyle;

        private GUIStyle titleStyle;

        [MenuItem("Window/Migration Tool")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow<MigrationWindow>("Migration Tool");
        }

        protected void OnEnable()
        {
            idExportPath = Application.dataPath + constants.RelativeExportPath;
            customOldIDSPath = EditorPrefs.GetString("MigrationTool.customOldIDSPath", customOldIDSPath);
            if (File.Exists(customOldIDSPath))
            {
                LoadCustomIDs();
            }
        }

        protected void OnDisable()
        {
            EditorPrefs.SetString("MigrationTool.customOldIDSPath", customOldIDSPath);
        }

        private void InitStyle()
        {
            titleStyle = new GUIStyle()
            {
                fontSize = 18,
                alignment = TextAnchor.UpperCenter,
                padding = new RectOffset(10, 10, 10, 50)
            };

            buttonStyle = GUI.skin.button;
            buttonStyle.wordWrap = true;
            buttonStyle.margin = new RectOffset(10, 10, 10, 10);
            buttonStyle.margin.right = 10;
            buttonStyle.margin.top = 10;
            buttonStyle.margin.bottom = 10;

            labelStyle = GUI.skin.label;
            labelStyle.wordWrap = true;
            labelStyle.margin = new RectOffset(10, 10, 10, 10);
        }

        void OnGUI()
        {
            InitStyle();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.LabelField("Migration Tool", titleStyle);
            GUILayout.Space(25);
            OnGuiDrawLineSeperator(25);

            exportExists = File.Exists(idExportPath);
            GUILayout.Label(exportExists
                ? "IDs found, ready to migrate."
                : "No IDs found, please export the current IDs.");
            if (GUILayout.Button((exportExists ? "Re-" : "") + "Export Classes \r\nof the current project"))
            {
                string rootPath = Application.dataPath;
                ThreadUtil.RunThread(() => { idExportView.ExportCurrentClassData(rootPath); });
            }

            GUILayout.Space(20);
            
            OnGuiDrawLineSeperator();

            EditorGUI.BeginDisabledGroup(!exportExists);
            if (GUILayout.Button("Migrate  scene \r\n to current project"))
            {
                sceneView.MigrateScene();
            }

            GUILayout.Space(20);

            OnGUIBatchProcessing();

            OnGUIAdvanced();

            GUILayout.Space(20);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndScrollView();
        }

        private void OnGuiDrawLineSeperator(int offset = 10)
        {
            var rect = EditorGUILayout.BeginHorizontal();
            Handles.color = Color.gray;
            Handles.DrawLine(new Vector2(rect.x + offset, rect.y), new Vector2(rect.width - offset, rect.y));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        private void OnGUIBatchProcessing()
        {
            EditorGUILayout.Separator();
            batchProcessingEnabled = EditorGUILayout.Foldout(batchProcessingEnabled, "Batch processing tools");
            if (!batchProcessingEnabled) return;

            OnGuiDrawLineSeperator();

            if (GUILayout.Button("Migrate all  prefabs \r\n from project folder"))
            {
                string rootPath = Application.dataPath;
                ThreadUtil.RunThread(() => { prefabView.MigrateAllPrefabs(rootPath); });
            }

            if (GUILayout.Button("Migrate all  scenes \r\n from project folder"))
            {
                sceneView.MigrateAllScenes();
            }

//            OnGuiDrawLineSeperator();
        }

        private void OnGUIAdvanced()
        {
            EditorGUILayout.Separator();
            advancedOptionsEnabled = EditorGUILayout.Foldout(advancedOptionsEnabled,
                "Advanced" + (!String.IsNullOrEmpty(customOldIDSPath) ? " [Custom IDs Enabled]" : ""));
            if (!advancedOptionsEnabled) return;
            OnGuiDrawLineSeperator();

            GUILayout.Label(
                "If you want to use a custom ID export of the old project, can select it here. If you do not know what that means, you shouldn't use this.");

            EditorGUILayout.Separator();

            GUILayout.Label(String.IsNullOrEmpty(customOldIDSPath)
                ? "No custom IDs path set, will use the default in the project."
                : "Old IDs being used : " + customOldIDSPath);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Select"))
            {
                customOldIDSPath =
                    EditorUtility.OpenFilePanel("Select custom Export.json", Application.dataPath, "json");
                if (string.IsNullOrEmpty(customOldIDSPath))
                {
                    customOldIDSPath = null;
                }
                else
                {
                    LoadCustomIDs();
                }


            }

            if (GUILayout.Button("Clear"))
            {
                customOldIDSPath = null;
            }

            GUILayout.EndHorizontal();
//            OnGuiDrawLineSeperator();
        }

        private void LoadCustomIDs()
        {
            Administration.Instance.oldIDsOverride = JsonConvert.DeserializeObject<List<ClassModel>>(File.ReadAllText(customOldIDSPath));
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