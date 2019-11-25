//using System;
//using UnityEditor;
//using UnityEngine;
//
//public class PREspectiveUpdaterWindow : EditorWindow
//{
//    class AfterImportScript : AssetPostprocessor
//    {
//        
//        //todo: Port to PREspective naming convention 
//        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
//            string[] movedFromAssetPaths)
//        {
//            if (PREspectiveUpdaterWindow.OnAssetsImported == null)
//            {
//                Debug.Log("Event was null, ignoring"); // todo : test code remove 
//            }
//            else
//            {
//                PREspectiveUpdaterWindow.OnAssetsImported.Invoke();
//                PREspectiveUpdaterWindow.OnAssetsImported = null;
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
//    public static Action OnAssetsImported;
//
//    private const string EXTENSION = "prepackage";
//
//    [MenuItem("Window/TEST_Updater_TEST")]
//    public static void ShowWindow()
//    {
//        EditorWindow.GetWindow<PREspectiveUpdaterWindow>("PREspective updater");
//    }
//
//    void OnGUI()
//    {
//        if (GUILayout.Button("migrate to prespective"))
//        {
//            MigrateProject();
//        }
//    }
//
//    private void MigrateProject()
//    {
//        EditorUtility.OpenFilePanel("Select PREpackage", Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
//            EXTENSION)
//    }
//}