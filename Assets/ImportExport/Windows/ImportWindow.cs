#if UNITY_EDITOR
using importerexporter.models;
using importerexporter.utility;
using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
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

        private Thread calculationThread;


        [MenuItem("Window/Scene import window")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(ImportWindow));
        }

        protected void OnEnable()
        {
            wordWrapStyle = new GUIStyle() {wordWrap = true, padding = new RectOffset(10, 10, 10, 10)};
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

            if (GUILayout.Button("test generate mapping"))
            {
                string IDPath = EditorUtility.OpenFilePanel(
                    "ID export (old project assets/ImportExport/Exports/Export.json)", Application.dataPath,
                    "json"); //todo : check if this is in the current project
                if (IDPath.Length != 0)
                {
                    List<ClassData> oldIDs = JsonConvert.DeserializeObject<List<ClassData>>(File.ReadAllText(IDPath));
                    List<ClassData> currentIDs = cachedLocalIds == null || cachedLocalIds.Count == 0
                        ? idUtility.ExportClassData(Application.dataPath)
                        : cachedLocalIds;

                    new Thread(() => {
                        List<FoundScript> foundScripts = new List<FoundScript>();
                        FoundScriptMappingGenerator.Instance.GenerateMapping(oldIDs, currentIDs, ref foundScripts);    
                    }).Start();
                    
                }
            }
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
                    "Can't Start new import while import is running", "Resume");
                return;
            }

            string IDPath = EditorUtility.OpenFilePanel(
                "ID export (old project assets/ImportExport/Exports/Export.json)", Application.dataPath,
                "json"); //todo : check if this is in the current project
            if (IDPath.Length != 0)
            {
                List<ClassData> oldIDs =
                    JsonConvert.DeserializeObject<List<ClassData>>(File.ReadAllText(IDPath));

                string scenePath =
                    EditorUtility.OpenFilePanel("Scene to import", IDPath + "/../../",
                        "unity"); //todo : check if this is in the current project
                if (scenePath.Length != 0)
                {
                    string rootPath = Application.dataPath;
                    calculationThread = new Thread(() => this.Import(rootPath, oldIDs, scenePath));
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

        private List<ClassData> cachedLocalIds;

        /// <summary>
        /// Make a copy of the scene file and change the GUIDs, fileIDs and if necessary the fields 
        /// </summary>
        /// <param name="scenePath"></param>
        private void Import(string rootPath, List<ClassData> oldIDs, string scenePath)
        {
            try
            {
                if (constants.DEBUG)
                {
                    Debug.LogWarning("[DEBUG ACTIVE] Using old ids for the import");
                }

                List<ClassData> currentIDs =
                    cachedLocalIds == null || cachedLocalIds.Count == 0
                        ? idUtility.ExportClassData(rootPath)
                        : cachedLocalIds;
                cachedLocalIds = currentIDs;

                List<FoundScript> foundScripts =
                    new List<FoundScript>();

                string[] lastSceneExport =
                    idUtility.ImportClassDataAndTransformIDs(scenePath, oldIDs, currentIDs,
                        ref foundScripts); //todo : don't use a ref for this because that's like super nasty

                
//                fieldMappingUtility.FindFieldsToMigrate(lastSceneExport, oldIDs, currentIDs, ref foundScripts); // todo this might be able to be removed

                Instance().Enqueue(() => { ImportMainThread(rootPath, scenePath, foundScripts, lastSceneExport); });
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }
        }

        private void
            ImportMainThread(string rootPath, string scenePath,
                List<FoundScript> foundScripts,
                string[] lastSceneExport) //todo : terrible name, rename! - 10-10-2019 - Wouter
        {
            ImportWindow.foundScripts = foundScripts;
            ImportWindow.lastSceneExport = lastSceneExport;

            foreach (FoundScript script in foundScripts)
            {
                if (script.HasBeenMapped == FoundScript.MappedState.NotChecked)
                {
                    throw new NotImplementedException("Script has not been checked for mapping");
                }
            }


            FoundScript[] unmappedScripts = foundScripts
                .Where(field => field.HasBeenMapped == FoundScript.MappedState.NotMapped).ToArray();
            if (unmappedScripts.Length > 0)
            {
                // Remove duplicate scripts
                List<FoundScript> scripts =
                    unmappedScripts
                        .GroupBy(field => field.NewClassData.Name)
                        .Select(group => group.First()).ToList();

                EditorUtility.DisplayDialog("Merging fields necessary",
                    "Could not merge all the fields to the class in the new project. You'll have to manually match old fields with the new fields",
                    "Open merge window");

                mergingWizard = MergingWizard.CreateWizard(scripts);

                mergingWizard.onComplete = (list) =>
                {
                    MergingWizardCompleted(list, rootPath, scenePath, lastSceneExport);
                };
            }
            else
            {
                SaveFile(rootPath + "/" + Path.GetFileName(scenePath), lastSceneExport);
                calculationThread = null;
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
        private void MergingWizardCompleted(List<FoundScript> mergeNodes, string rootPath,
            string scenePath,
            string[] linesToChange)
        {
            string[] newSceneExport =
                fieldMappingUtility.ReplaceFieldsByMergeNodes(linesToChange, mergeNodes);

            Debug.Log(string.Join("\n", newSceneExport));

            SaveFile(rootPath + "/" + Path.GetFileName(scenePath), linesToChange);
        }

        #region ThreadedUI

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

        #endregion
    }
}
#endif