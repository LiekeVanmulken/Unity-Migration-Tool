using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using migrationtool.models;
using migrationtool.utility;
using migrationtool.views;
using migrationtool.windows;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using SceneView = migrationtool.views.SceneView;

namespace u040.prespective.migrationtoool
{
    public class PREpackageImporter
    {
        private const string EXTENSION = "prepackage";
        public const string PREPACKAGE_PACKAGE_LOCATION = "MIGRATION_TOOL_UPDATER.PACKAGE_IMPORTING";
        public const string PREPACKAGE_PACKAGE_CONTENT = "MIGRATION_TOOL_UPDATER.PACKAGE_CONTENT";

        private static SceneView sceneView = new SceneView();
        private static PrefabView prefabView = new PrefabView();
        private static IDExportView IDView = new IDExportView();

        void OnGUI()
        {
            if (GUILayout.Button("Import package"))
            {
                ImportPackage();
            }
        }

        [MenuItem("Assets/PREpackage/Import")]
        private static void ImportPackage() 
        {
            if (!EditorUtility.DisplayDialog("BACKUP YOUR PROJECT!",
                "Please BACKUP your project before proceeding. A faulty migration can lead to DATA LOSS!",
                "I've made a backup!", "I will make a backup now"))
            {
                return;
            }
//
//            if (PlayerPrefs.HasKey(PREPACKAGE_PACKAGE_LOCATION))
//            {
//                Debug.LogError("Packager already running");
//                return;
//            }

            Administration.Instance.ShowInfoPopups = false;

            string packageLocation = getPackageLocation();
            if (string.IsNullOrEmpty(packageLocation))
            {
                Debug.Log("No package selected.");
                return;
            }
            PlayerPrefs.SetString(PREPACKAGE_PACKAGE_LOCATION, packageLocation);

            string rootPath = Application.dataPath;
            ThreadUtil.RunThread(() =>
            {
                packageImportStarted(rootPath, packageLocation);
                List<string> files = Unzipper.ParseUnityPackagesToFiles(packageLocation);
                string packageContent = string.Join(",", files);
                ThreadUtil.RunMainThread(() =>
                {
                    AssetDatabase.ImportPackage(packageLocation, true);
                    PlayerPrefs.SetString(PREPACKAGE_PACKAGE_LOCATION, packageLocation);
                    PlayerPrefs.SetString(PREPACKAGE_PACKAGE_CONTENT, packageContent);
                });
            });
        }

        private static string getPackageLocation()
        {
            return EditorUtility.OpenFilePanel("Select a prepackage",
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop), EXTENSION);
        }

        private static void packageImportStarted(string projectPath, string packageLocation)
        {
            Debug.LogWarning("[PREpackage] Package import Started");
            IDView.ExportCurrentClassData(projectPath);
        }

        public static void packageImportFinished(string projectPath, string packageLocation)
        {
            Debug.LogWarning("[PREpackage] Project migration started ");

            Constants constants = Constants.Instance;

            string customOldIDSPath = projectPath + constants.RelativeExportPath;
            if (!File.Exists(customOldIDSPath))
            {
                Debug.LogError("Could not find old PREspective IDs");
                return;
            }

            Administration.Instance.oldIDsOverride =
                JsonConvert.DeserializeObject<List<ClassModel>>(File.ReadAllText(customOldIDSPath));
            string moveTo = projectPath + constants.RelativeExportPath.Replace(".json", "_old.json");
            if (File.Exists(moveTo))
            {
                File.Move(moveTo, ProjectPathUtility.AddTimestamp(moveTo));
            }

            File.Move(customOldIDSPath, moveTo);

            IDView.ExportCurrentClassData(projectPath);
            ThreadUtil.RunMainThread(() =>
            {
                Administration.Instance.OverWriteMode = true;
                Administration.Instance.ShowInfoPopups = false;
                Administration.Instance.MigrateScenePrefabDependencies = false;

                Action onCompleteScenes = () =>
                {
                    ThreadUtil.RunMainThread(() =>
                    {
                        Administration.Instance.OverWriteMode = false;
                        Administration.Instance.ShowInfoPopups = true;
                        Administration.Instance.MigrateScenePrefabDependencies = true;
                        
                        EditorUtility.DisplayDialog("Migration completed",
                            "Completed the migration, everything should be migrated to the new version. \r\nPlease check your project for any errors.",
                            "Ok");
                    });
                };
                Action onCompletePrefabs = () =>
                {
                    ThreadUtil.RunMainThread(() => { sceneView.MigrateAllScenes(projectPath, onCompleteScenes); });
                };
                ThreadUtil.RunThread(() =>
                {
                    prefabView.MigrateAllPrefabs(projectPath, projectPath, onCompletePrefabs);
                });
            });
        }
    }
}