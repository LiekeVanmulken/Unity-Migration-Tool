//using System;
//using System.Collections.Generic;
//using System.IO;
//using migrationtool.models;
//using migrationtool.views;
//using migrationtool.windows;
//using Newtonsoft.Json;
//using UnityEditor;
//using UnityEngine;
//using SceneView = migrationtool.views.SceneView;
//
//namespace u040.migrationtoool
//{
//    class AfterImportScript : AssetPostprocessor
//    {
//        //todo: Port to PREspective naming convention 
//        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
//            string[] movedFromAssetPaths)
//        {
//            if (PrespectivePackagesImporter.OnAssetsImported == null)
//            {
//                Debug.Log("Event was null, ignoring"); // todo : test code remove 
//            }
//            else
//            {
//                PrespectivePackagesImporter.OnAssetsImported.Invoke();
//                PrespectivePackagesImporter.OnAssetsImported = null;
//            }
//
//            foreach (string str in importedAssets)
//            {
//                Debug.Log("Reimported Asset: " + str);
//            }
//
//            foreach (string str in deletedAssets)
//            {
//                Debug.Log("Deleted Asset: " + str);
//            }
//
//            for (int i = 0; i < movedAssets.Length; i++)
//            {
//                Debug.Log("Moved Asset: " + movedAssets[i] + " from: " + movedFromAssetPaths[i]);
//            }
//        }
//    }
//
//    public class PrespectivePackagesImporter : EditorWindow
//    {
//        [MenuItem("Assets/Do Something", true)]
//        private static bool DoSomethingValidation()
//        {
//            return Selection.activeObject.GetType() == typeof(MonoScript);
//        }
//
//
//        public static Action OnAssetsImported;
//
//        private const string EXTENSION = "prepackage";
//
//        private static SceneView sceneView = new SceneView();
//        private static IDExportView IDView = new IDExportView();
//
//
//        [MenuItem("Window/Import Prespective", true)]
//        static void Init()
//        {
//            // Get existing open window or if none, make a new one:
//            PrespectivePackagesImporter window =
//                (PrespectivePackagesImporter) EditorWindow.GetWindow(typeof(PrespectivePackagesImporter));
//            window.Show();
//        }
//
//        void OnGUI()
//        {
//            if (GUILayout.Button("Import package"))
//            {
//                ImportPackage();
//            }
//
//            if (GUILayout.Button("Export package"))
//            {
//                ExportPackage();
//            }
//        }
//
//
//        [MenuItem("Assets/PREpackage/Export")]
//        private static void ExportPackage()
//        {
//            Debug.Log("You did something!");
//
//            string exportPath = EditorUtility.OpenFilePanel("Select a prepackage", Application.dataPath, EXTENSION);
//
//
//            AssetDatabase.ExportPackage();
//        }
//
////        [MenuItem("Assets/PREpackage/Export", true)]
////        private static bool ExportPackageValidation()
////        {
////            return false;
////        }
//
//        [MenuItem("Assets/PREpackage/Import")]
//        private static void ImportPackage()
//        {
//            Debug.Log("Import package called");
//
//
//            Administration.Instance.OverwriteFiles = true;
//            string packageLocation = getPackageLocation();
//            if (string.IsNullOrEmpty(packageLocation))
//            {
//                Debug.Log("No package selected.");
//                return;
//            }
//
//            IDView.ExportCurrentClassData(Application.dataPath);
//
//            packageImportStarted(packageLocation);
//            AssetDatabase.ImportPackage(packageLocation, true);
//            OnAssetsImported = () =>
//            {
//                Debug.LogWarning("On Assets imported called");
//                packageImportFinished(packageLocation);
//                Administration.Instance.OverwriteFiles = false;
//            };
//        }
//
//        [MenuItem("Assets/PREpackage/Import", true)]
//        private static bool ImportPackageValidation()
//        {
//            return true;
//        }
//
//
//        private static string getPackageLocation()
//        {
////            string package = @"C:\Users\ArtPC-009\Desktop\delete_me\Prespective81.prepackage";
//            return EditorUtility.OpenFilePanel("Select a prepackage",
//                Environment.GetFolderPath(Environment.SpecialFolder.Desktop), EXTENSION);
//        }
//
//
//        private static void packageImportStarted(string packageLocation)
//        {
//        }
//
//        private static void packageImportFinished(string packageLocation)
//        {
//            // get the exports.json
//            string customOldIDSPath = Application.dataPath + "/Plugins/PREspective/Export.json";
//
//            if (!File.Exists(customOldIDSPath))
//            {
//                Debug.LogError("Could not find old PREspective IDs");
//                return;
//            }
//
//            Administration.Instance.oldIDsOverride =
//                JsonConvert.DeserializeObject<List<ClassModel>>(File.ReadAllText(customOldIDSPath));
//
//            migrateScenes();
//        }
//
//        private static void migrateScenes()
//        {
//            string path = Application.dataPath + "/Plugins/PREspective/";
//            string[] sceneFiles = Directory.GetFiles(path, "*.unity", SearchOption.AllDirectories);
//
//            foreach (string scene in sceneFiles)
//            {
//                sceneView.MigrateScene(scene);
//            }
//
//            Debug.Log("Migrated all scenes");
//        }
//    }
//}