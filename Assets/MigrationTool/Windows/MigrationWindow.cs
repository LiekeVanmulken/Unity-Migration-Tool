#if UNITY_EDITOR
using migrationtool.utility;
using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Threading;
using migrationtool.controllers;
using migrationtool.views;
using SceneView = migrationtool.views.SceneView;

namespace migrationtool.windows
{
    /// <summary>
    /// Main entry point to the Migration Tool
    /// 
    /// For a more detailed explanation read the README.MD
    /// </summary>
    [Serializable]
    public class MigrationWindow : EditorWindow
    {
        private readonly Constants constants = Constants.Instance;
        private readonly Administration administration = Administration.Instance;

        private readonly IDExportView idExportView = new IDExportView();
        private readonly SceneView sceneView = new SceneView();
        private readonly PrefabView prefabView = new PrefabView();
        private readonly MappingView mappingView = new MappingView();

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

        /// <summary>
        /// Style for the button
        /// </summary>
        private GUIStyle buttonStyle;

        /// <summary>
        /// Style for the label
        /// </summary>
        private GUIStyle labelStyle;

        /// <summary>
        /// Style for the title
        /// </summary>
        private GUIStyle titleStyle;

        [MenuItem("Window/Migration Tool")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow<MigrationWindow>("Migration Tool");
        }

        protected void OnEnable()
        {
            idExportPath = constants.RootDirectory + constants.RelativeExportPath;
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

        /// <summary>
        /// Init the styles
        /// </summary>
        private void InitStyle()
        {
            titleStyle = new GUIStyle()
            {
                fontSize = 18,
                alignment = TextAnchor.UpperCenter,
                padding = new RectOffset(10, 10, 10, 50)
            };

            if (EditorPrefs.GetInt("UserSkin") == 1)
            {
                titleStyle.normal.textColor = new Color(209, 209, 209);
            }
            if (administration.OverWriteMode)
            {
                titleStyle.normal.textColor = Color.red;
            }
            

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

        /// <summary>
        /// Render the GUI
        /// </summary>
        void OnGUI()
        {
            InitStyle();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.LabelField("Migration Tool"  + (administration.OverWriteMode ? " [OverWriting]" : ""), titleStyle);
            GUILayout.Space(25);
            OnGuiDrawLineSeparator(25);

            exportExists = File.Exists(idExportPath);
            GUILayout.Label(exportExists
                ? "IDs found, ready to migrate."
                : "No IDs found, please export the current IDs.");
            if (GUILayout.Button((exportExists ? "Re-" : "") + "Export Classes \r\nof the current project"))
            {
                ThreadUtil.RunThread(() => { idExportView.ExportCurrentClassData(constants.RootDirectory); });
            }

            GUILayout.Space(20);
            OnGuiDrawLineSeparator();

            EditorGUI.BeginDisabledGroup(!exportExists);
            if (GUILayout.Button("Migrate  scene \r\n to current project"))
            {
                ThreadUtil.RunThread(() =>
                {
                    sceneView.MigrateScene();
                });
            }

            GUILayout.Space(20);

            OnGUIBatchProcessing();
            OnGUIAdvanced();

            GUILayout.Space(20);
            OnGuiDrawLineSeparator();
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Render the batch processing part of the ui
        /// </summary>ww 
        private void OnGUIBatchProcessing()
        {
            EditorGUILayout.Separator();
            batchProcessingEnabled = EditorGUILayout.Foldout(batchProcessingEnabled, "Batch processing tools");
            if (!batchProcessingEnabled) return;

            OnGuiDrawLineSeparator();

            if (GUILayout.Button("Migrate all  prefabs \r\n from folder to current project"))
            {
                ThreadUtil.RunThread(() => { prefabView.MigrateAllPrefabs(constants.RootDirectory + "/Assets"); });
            }

            if (GUILayout.Button("Migrate all  scenes \r\n from folder to current project"))
            {
                sceneView.MigrateAllScenes();
            }
        }

        /// <summary>
        /// Render the Advanced part of the ui
        /// </summary>
        private void OnGUIAdvanced()
        {
            EditorGUILayout.Separator();
            advancedOptionsEnabled = EditorGUILayout.Foldout(advancedOptionsEnabled,
                "Advanced" + (!String.IsNullOrEmpty(customOldIDSPath) ? " [Custom IDs Enabled]" : ""));
            if (!advancedOptionsEnabled) return;
            OnGuiDrawLineSeparator();

            GUILayout.Label(
                "If you want to use a custom ID export of the old project, you can select it here. If you do not know what that means, you shouldn't use this.");

            EditorGUILayout.Separator();

            GUILayout.Label(String.IsNullOrEmpty(customOldIDSPath)
                ? "No custom IDs path set, will use the default in the old project (" + constants.RelativeExportPath + ")."
                : "Old IDs being used : " + customOldIDSPath);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Select"))
            {
                customOldIDSPath =
                    EditorUtility.OpenFilePanel("Select custom Export.json", constants.RootDirectory, "json");
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

            EditorGUILayout.Separator();
            OnGuiDrawLineSeparator();

            GUILayout.Label("Generate a mapping from an Old and New ID export in advance.");
            if (GUILayout.Button("Generate Mapping"))
            {
                mappingView.MapAllClasses();
            }
            EditorGUILayout.Separator();
            OnGuiDrawLineSeparator();
            EditorGUILayout.Separator();
            
            bool cacheOverWriteMode =
                GUILayout.Toggle(administration.OverWriteMode, "Overwrite scenes and prefabs instead of making copies.");
            if (cacheOverWriteMode && !administration.OverWriteMode)
            {
                if (!EditorUtility.DisplayDialog("OVERWRITE MODE : ENABLED",
                    "OVERWRITE MODE engaged. \r\n\r\nMAKE SURE YOU HAVE A BACKUP!", "I HAVE A BACKUP!",
                    "I'm going to make one now"))
                {
                    cacheOverWriteMode = false;
                    Debug.LogWarning("Please make a backup. Losing files and meta files would break the project irreversibly.");
                }
                else
                {
                    Debug.LogError("[OVERWRITE MODE] ENABLED. Please backup your project. This can break things irreversibly.\r\n");
                }
            }
            else if (cacheOverWriteMode != administration.OverWriteMode)
            {
                Debug.Log("[OVERWRITE MODE] DISABLED");
            }
            administration.OverWriteMode = cacheOverWriteMode;
            
            
            EditorGUILayout.Separator();
            OnGuiDrawLineSeparator();
            EditorGUILayout.Separator();
            
            
            administration.ShowInfoPopups=
                GUILayout.Toggle(administration.ShowInfoPopups, "Show info popups.");
            administration.MigrateScenePrefabDependencies=
                GUILayout.Toggle(administration.MigrateScenePrefabDependencies, "Migrate prefab dependencies of scenes.");
            
            EditorGUILayout.Separator();
            OnGuiDrawLineSeparator();
            EditorGUILayout.Separator();

            GUILayout.Label("Debug");
            EditorGUI.BeginDisabledGroup(true);
            GUILayout.Toggle(administration.newIDsOverride != null, "New IDs overriden.");
            GUILayout.Toggle(administration.ScriptMappingsOverride != null, "Script mappings overriden.");
            EditorGUI.EndDisabledGroup();
        }

        /// <summary>
        /// Draw a line as a separator
        /// </summary>
        /// <param name="offset"></param>
        private void OnGuiDrawLineSeparator(int offset = 10)
        {
            var rect = EditorGUILayout.BeginHorizontal();
            Handles.color = Color.gray;
            Handles.DrawLine(new Vector2(rect.x + offset, rect.y), new Vector2(rect.width - offset, rect.y));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }


        /// <summary>
        /// Load custom IDs as old IDs
        /// </summary>
        private void LoadCustomIDs()
        {
            Administration.Instance.oldIDsOverride = IDController.DeserializeIDs(customOldIDSPath);
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
            ThreadUtil.RunMainThread(() =>
            {
                Action<string> onComplete = wizardResult =>
                {
                    result = wizardResult;
                    completed = true;
                };
                Action onIgnore = () => { completed = true; };

                OptionsWizard.CreateWizard(label, original, options, onComplete, onIgnore);
            });

            while (!completed)
            {
                Thread.Sleep(Constants.Instance.THREAD_WAIT_TIME);
            }

            return result;
        }

        /// <summary>
        /// Display a dialog from a different thread
        /// </summary>
        /// <param name="title"></param>
        /// <param name="info"></param>
        public static void DisplayDialog(string title, string info)
        {
            ThreadUtil.RunMainThread(() => EditorUtility.DisplayDialog(title, info, "Ok"));
        }

        /// <summary>
        /// Display the progressbar from a different thread
        /// </summary>
        /// <param name="title"></param>
        /// <param name="info"></param>
        /// <param name="progress"></param>
        public static void DisplayProgressBar(string title, string info, float progress)
        {
            ThreadUtil.RunMainThread(() => EditorUtility.DisplayProgressBar(title, info, progress));
        }

        /// <summary>
        /// Clear the progressbar from a different thread
        /// </summary>
        public static void ClearProgressBar()
        {
            ThreadUtil.RunMainThread(EditorUtility.ClearProgressBar);
        }

        #endregion
    }
}
#endif