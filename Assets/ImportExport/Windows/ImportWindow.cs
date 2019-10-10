#if UNITY_EDITOR
using importerexporter.models;
using importerexporter.utility;
using System.Linq;
using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Threading;

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
    public class ImportWindow : MainThreadDispatcherEditorWindow
    {
        private readonly Constants constants = Constants.Instance;
        private readonly IDUtility idUtility = IDUtility.Instance;
        private readonly FieldMappingUtility fieldMappingUtility = FieldMappingUtility.Instance;

        private static List<ClassData> oldFileDatas;

        private static string[] lastSceneExport;
        private static List<FoundScript> foundScripts;

        private GUIStyle wordWrapStyle;

        private static MergingWizard mergingWizard;
//        private string jsonField;

        private Thread calculationThread;

        private List<KeyValuePair<string, bool>> dllFiles;

        private string progressBarMessage;
//        private Vector2 dllFilesScroll = Vector2.zero;


        [MenuItem("Window/Scene import window")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(ImportWindow));
        }

        protected void OnEnable()
        {
            wordWrapStyle = new GUIStyle() {wordWrap = true, padding = new RectOffset(10, 10, 10, 10)};
//            dllFiles = new List<KeyValuePair<string, bool>>();     
//            var dllFilePaths = Directory.GetFiles(Application.dataPath, "*" +".dll", SearchOption.AllDirectories);
//            foreach (string dllFilePath in dllFilePaths)
//            {
//                dllFiles.Add(new KeyValuePair<string, bool>(dllFilePath, true));
//            }
        }

        void OnGUI()
        {
            if (GUILayout.Button("Export Class Data of the current project"))
            {
                string rootPath = Application.dataPath;
                new Thread(() => { ExportCurrentClassData(rootPath); }
                ).Start(); // todo : put this somewhere else
            }

            if (GUILayout.Button("Import Class Data and scene"))
            {
                ImportClassDataAndScene();
            }

//            EditorGUILayout.LabelField("Dll to map classes from");
//            EditorGUILayout.BeginScrollView(dllFilesScroll);
//            for (var i = 0; i < dllFiles.Count; i++)
//            {
//                KeyValuePair<string, bool> pair = dllFiles[i];
//
//
//                EditorGUILayout.BeginHorizontal();
//
//                dllFiles[i] = new KeyValuePair<string, bool>(pair.Key, EditorGUILayout.Toggle(pair.Value));
//                EditorGUILayout.LabelField(pair.Key.Replace(Application.dataPath, ""), wordWrapStyle);
//
//                EditorGUILayout.EndHorizontal();
//            }
//
//            EditorGUILayout.EndScrollView();
        }

        private void ExportCurrentClassData(string rootPath)
        {
            List<ClassData> oldIDs = idUtility.ExportClassData(rootPath);
            var jsonSerializerSettings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                Formatting = Formatting.Indented
            };

            var jsonField = JsonConvert.SerializeObject(oldIDs, jsonSerializerSettings);
            string filePath = rootPath + "/ImportExport/Exports/Export.json";
            File.WriteAllText(filePath, jsonField);

            DisplayDialog("Export complete",
                "All classes were exported to " + filePath + " . Open up the new project and import the scene.");
        }

        private void ImportClassDataAndScene()
        {
            if (calculationThread != null)
            {
                EditorUtility.DisplayDialog("Already running import",
                    "Can't Start new import while import is running", "Ok");
                return;
            }

            string IDPath = EditorUtility.OpenFilePanel(
                "ID export (old project assets/ImportExport/Exports/Export.json)", Application.dataPath,
                "*"); //todo : check if this is in the current project
            if (IDPath.Length != 0)
            {
                List<ClassData> oldIDs = ClassData.Parse(File.ReadAllText(IDPath));

                string scenePath =
                    EditorUtility.OpenFilePanel("Scene to import", Application.dataPath,
                        "*"); //todo : check if this is in the current project
                if (scenePath.Length != 0)
                {
                    string rootPath = Application.dataPath;
                    calculationThread = new Thread(() => this.Import(rootPath, oldIDs, scenePath,
                        (message) => progressBarMessage = message));
                    calculationThread.Start();
                }
                else
                {
                    Debug.LogWarning("No path was selected");
                }
            }
            else
            {
                Debug.LogWarning("No path was selected");
            }
        }

        /// <summary>
        /// Make a copy of the scene file and change the GUIDs, fileIDs and if necessary the fields 
        /// </summary>
        /// <param name="scenePath"></param>
        private void Import(string rootPath, List<ClassData> oldIDs, string scenePath, Action<string> setProgressBar)
        {
            try
            {
//            List<ClassData> oldIDs = idUtility.ExportClassData(oldProjectPath);
                List<ClassData> currentIDs =
                    constants.DEBUG ? oldIDs : idUtility.ExportClassData(rootPath);

                var lastSceneExport =
                    idUtility.ImportClassDataAndTransformIDs(rootPath, scenePath, oldIDs, currentIDs);

                var foundScripts = fieldMappingUtility.FindFieldsToMigrate(lastSceneExport, oldIDs, currentIDs);

                Instance().Enqueue(() => { ImportMainThread(rootPath, scenePath, foundScripts, lastSceneExport); });
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }
        }

        private void
            ImportMainThread(string rootPath, string scenePath, List<FoundScript> foundScripts,
                string[] lastSceneExport) //todo : terrible name, rename! - 10-10-2019 - Wouter
        {
            ImportWindow.foundScripts = foundScripts;
            ImportWindow.lastSceneExport = lastSceneExport;

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
                    MergingWizardCompleted(list, rootPath, scenePath, lastSceneExport);
                };
            }
            else
            {
                SaveFile(rootPath + "/" + Path.GetFileName(scenePath), lastSceneExport);
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
            File.WriteAllText(newScenePath, string.Join("\n", linesToWrite));
            EditorUtility.DisplayDialog("Imported data", "The scene was exported to " + newScenePath, "Ok");
            GUIUtility.systemCopyBuffer = newScenePath;
        }

        /// <summary>
        /// Change the fields after merging with the merging window
        /// </summary>
        /// <param name="mergeNodes"></param>
        /// <param name="rootPath"></param>
        /// <param name="scenePath"></param>
        /// <param name="linesToChange"></param>
        private void MergingWizardCompleted(List<FoundScript> mergeNodes, string rootPath, string scenePath,
            string[] linesToChange)
        {
            string[] newSceneExport =
                fieldMappingUtility.ReplaceFieldsByMergeNodes(linesToChange, mergeNodes);

            Debug.Log(string.Join("\n", newSceneExport));

            SaveFile(rootPath + "/"+ Path.GetFileName(scenePath), linesToChange);
        }

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

                OptionsWizard optionsWizard =
                    OptionsWizard.CreateWizard(label, original, options, onComplete, onIgnore);
            });

            while (!completed)
            {
                Thread.Sleep(100);
            }

            Debug.Log("OptionsWindow result : " + result);
            return result;
        }

        public static void DisplayDialog(string title, string info)
        {
            Instance().Enqueue(() => { EditorUtility.DisplayDialog(title, info, "Ok"); });
        }

        public static void DisplayProgressBar(string title, string info, float progress)
        {
            Instance().Enqueue(() => { EditorUtility.DisplayProgressBar(title, info, progress); });
        }

        public static void ClearProgressBar()
        {
            Instance().Enqueue(EditorUtility.ClearProgressBar);
        }
    }
}
#endif