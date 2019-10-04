#if UNITY_EDITOR
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using importerexporter.models;
using importerexporter.utility;
using System.Linq;
using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.CodeDom;

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
    public class ImportWindow : EditorWindow
    {
        [SerializeField] private static string oldProjectPath;
        private readonly IDUtility idUtility = IDUtility.Instance;
        private readonly FieldMappingUtility fieldMappingUtility = FieldMappingUtility.Instance;
        private GUIStyle wordWrapStyle;


        [MenuItem("Window/Scene import window")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(ImportWindow));
        }

        private const string EDITORPREFS_KEY = "ImportExportWindow";

        protected void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;
            oldProjectPath = EditorPrefs.GetString(EDITORPREFS_KEY);
            wordWrapStyle = new GUIStyle() {wordWrap = true, padding = new RectOffset(10, 10, 10, 10)};
        }

        protected void OnDisable()
        {
            EditorPrefs.SetString(EDITORPREFS_KEY, oldProjectPath);
        }


        /// <summary>
        /// Position of the scroll for the UI
        /// </summary>
        private Vector2 scrollPosition;

        private static List<ClassData> oldFileDatas;
        private static string[] lastSceneExport;
        private static List<FoundScript> foundScripts;
        private static MergingWizard mergingWizard;
        private Constants constants = Constants.Instance;

        private string jsonField;
        void OnGUI() 
        {
            if (GUILayout.Button("Export IDs"))
            {
                List<ClassData> oldIDs = idUtility.ExportClassData(Application.dataPath);//todo : change?
                var jsonSerializerSettings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    Formatting = Formatting.Indented
                };
                jsonField = JsonConvert.SerializeObject(oldIDs, jsonSerializerSettings);
                File.WriteAllText(Application.dataPath + "/test.json", jsonField);
//                List<ClassData> test = JsonConvert.DeserializeObject<List<ClassData>>(jsonField);
                List<ClassData> classDatas = ClassData.Parse(jsonField);
                
                GUIUtility.systemCopyBuffer = jsonField;
            }
//            if (GUILayout.Button("export IDs"))
//            {
//                List<ClassData> oldIDs = idUtility.ExportClassData(oldProjectPath);
//                EditorUtility.DisplayProgressBar("Serializing json", "Serializing json", 0.2f);
//
//                var jsonSerializerSettings = new JsonSerializerSettings
//                {
//                    ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
//                    PreserveReferencesHandling = PreserveReferencesHandling.Objects
//                };
//                string json = JsonConvert.SerializeObject(oldIDs, jsonSerializerSettings);
//                List<ClassData> test = JsonConvert.DeserializeObject<List<ClassData>>(json, jsonSerializerSettings);
//                EditorUtility.ClearProgressBar();
//                Debug.Log(json);
//            }


//            GUILayout.Label("Old Assets folder : \n" + oldProjectPath, wordWrapStyle);
//            if (GUILayout.Button("Set old project path"))
//            {
//                string path = EditorUtility.OpenFolderPanel("title", Application.dataPath, "");
//                if (path.Length != 0)
//                {
//                    oldProjectPath = path;
//                }
//                
//            }

            EditorGUI.BeginDisabledGroup(String.IsNullOrEmpty(oldProjectPath));
            if (GUILayout.Button("Import IDs"))
            {
//                if (string.IsNullOrEmpty(oldProjectPath))
//                {
//                    if (EditorUtility.DisplayDialog("New Project import window",
//                        "Please select the path of the old project before proceeding.",
//                        "Ok"))
//                    {
//                        return;
//                    }
//                }

                string scenePath = EditorUtility.OpenFilePanel("Scene to import", Application.dataPath, "*"); //todo : check if this is in the current project
                if (scenePath.Length != 0)
                {
                    List<ClassData> oldIDs = JsonConvert.DeserializeObject<List<ClassData>>(jsonField);
                    Import(oldIDs, scenePath);
                }
                else
                {
                    Debug.LogWarning("No path was selected");
                }
            }
            EditorGUI.EndDisabledGroup();
//            jsonField = EditorGUILayout.TextArea(jsonField);
        }

        /// <summary>
        /// Make a copy of the scene file and change the GUIDs, fileIDs and if necessary the fields 
        /// </summary>
        /// <param name="scenePath"></param>
        private void Import(List<ClassData> oldIDs, string scenePath)
        {
//            List<ClassData> oldIDs = idUtility.ExportClassData(oldProjectPath);
            List<ClassData> currentIDs =
                constants.DEBUG ? oldIDs : idUtility.ExportClassData(Application.dataPath);

            lastSceneExport =
                idUtility.ImportClassDataAndTransformIDs(scenePath, oldIDs, currentIDs);

            foundScripts = fieldMappingUtility.FindFieldsToMigrate(lastSceneExport, oldIDs, currentIDs);


            if (foundScripts.Count > 0)
            {
                List<FoundScript> scripts =
                    foundScripts.Where(field => !field.HasBeenMapped).GroupBy(field => field.ClassData.Name)
                        .Select(group => group.First()).ToList();

                EditorUtility.DisplayDialog("Merging fields necessary",
                    "Could not merge all the fields to the class in the new project. You'll have to manually match old fields with the new fields",
                    "Open merge window");

                mergingWizard = MergingWizard.CreateWizard(scripts);

                mergingWizard.onComplete += (sender, list) =>
                {
                    MergingWizardCompleted(list, scenePath, lastSceneExport);
                };
            }
            else
            {
                SaveFile(scenePath, lastSceneExport);
            }
        }

        /// <summary>
        /// Saves the <param name="linesToWrite"/> to a new file at the <param name="scenePath"/>
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
        private void MergingWizardCompleted(List<FoundScript> mergeNodes, string scenePath,
            string[] linesToChange)
        {
            string[] newSceneExport =
                fieldMappingUtility.ReplaceFieldsByMergeNodes(linesToChange, mergeNodes);

            Debug.Log(string.Join("\n", newSceneExport));

            SaveFile(scenePath, linesToChange);
        }
    }
}
#endif