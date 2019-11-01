using importerexporter.controllers;
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
        private readonly IDController idController = IDController.Instance;
        private readonly FieldMappingController fieldMappingController = FieldMappingController.Instance;


        private static List<ClassModel> oldFileDatas;
        private static string[] lastSceneExport;
        private static List<FoundScript> foundScripts;

        private GUIStyle wordWrapStyle;


        private static MergingWizard mergingWizard;
        private Thread calculationThread;

        private string idExportPath;
        bool exportExists;


        [MenuItem("Window/Scene import window")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(ImportWindow));
        }

        protected void OnEnable()
        {
            wordWrapStyle = new GUIStyle() {wordWrap = true, padding = new RectOffset(10, 10, 10, 10)};
            idExportPath = Application.dataPath + "/ImportExport/Exports/Export.json";
        }

        void OnGUI()
        {
            if (GUILayout.Button("Export Classes of the current project"))
            {
                string rootPath = Application.dataPath;

                new Thread(
                    () => { ExportCurrentClassData(rootPath); }
                ).Start();
            }

            exportExists = File.Exists(idExportPath);

            EditorGUI.BeginDisabledGroup(!exportExists);
            if (GUILayout.Button("Migrate scene"))
            {
                ImportClassDataAndScene();
            }

            EditorGUI.EndDisabledGroup();

            GUILayout.Label(exportExists ? "IDs found, ready to migrate." : "No IDs found, please export the current IDs.");
        }

        private void ExportCurrentClassData(string rootPath)
        {
            List<ClassModel> oldIDs = idController.ExportClassData(rootPath);
            var jsonSerializerSettings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                Formatting = Formatting.Indented
            };

            string jsonField = JsonConvert.SerializeObject(oldIDs, jsonSerializerSettings);

            File.WriteAllText(idExportPath, jsonField);

            DisplayDialog("Export complete",
                "All classes were exported to " + idExportPath + " . Open up the new project and import the scene.");
        }

        private void ImportClassDataAndScene()
        {
            if (calculationThread != null)
            {
                if (!EditorUtility.DisplayDialog("Already running import",
                    "Can't Start new import while import is running", "Resume", "Stop"))
                {
                    calculationThread.Abort();
                    calculationThread = null;
                }

                return;
            }

            EditorUtility.DisplayDialog("Please select the Export.json", "Please select the Export.json of the old project in the next file dialog.\n" +
                                                                         "This can be found in the old_project_folder/ImportExport/Exports/Export.json.", "Select the original IDs");
            
            string IDPath = EditorUtility.OpenFilePanel(
                "ID export (old project assets/ImportExport/Exports/Export.json)", Application.dataPath,
                "json"); //todo : check if this is in the current project
            if (IDPath.Length == 0)
            {
                Debug.LogWarning("No path was selected");
                return;
            }
            
            List<ClassModel> oldIDs =
                JsonConvert.DeserializeObject<List<ClassModel>>(File.ReadAllText(IDPath));

            IDPath = Path.GetDirectoryName(IDPath) ;
            IDPath = Path.GetFullPath(Path.Combine(IDPath , @"..\..\"));
            EditorUtility.DisplayDialog("Please select the scene", "Please select the scene to migrate.", "Select the scene");
            string scenePath =
                EditorUtility.OpenFilePanel("Scene to import", IDPath,
                    "unity"); //todo : check if this is in the current project
            if (scenePath.Length == 0)
            {
                Debug.LogWarning("No path was selected");
                return;
            }

            string rootPath = Application.dataPath;
            string newIDsPath = rootPath + "/ImportExport/Exports/Export.json";

            List<ClassModel> newIDs = File.Exists(newIDsPath)
                ? JsonConvert.DeserializeObject<List<ClassModel>>(File.ReadAllText(newIDsPath))
                : idController.ExportClassData(rootPath);

            List<FoundScript> foundScripts = new List<FoundScript>();
            string foundScriptsPath = rootPath + "/ImportExport/Exports/Found.json";
            if (File.Exists(foundScriptsPath))
            {
                foundScripts =
                    JsonConvert.DeserializeObject<List<FoundScript>>(File.ReadAllText(foundScriptsPath));
            }

            calculationThread =
                new Thread(() => this.ImportTransformIDs(rootPath, oldIDs, newIDs, scenePath, foundScripts));
            calculationThread.Start();
        }

        private List<ClassModel> cachedLocalIds;

        /// <summary>
        /// Make a copy of the scene file and change the GUIDs, fileIDs and if necessary the fields 
        /// </summary>
        /// <param name="scenePath"></param>
        private void ImportTransformIDs(string rootPath, List<ClassModel> oldIDs, List<ClassModel> currentIDs, string scenePath,
            List<FoundScript> foundScripts)
        {
            try
            {
                if (constants.DEBUG)
                {
                    Debug.LogWarning("[DEBUG ACTIVE] Using old ids for the import");
                }

                if (oldIDs == null || currentIDs == null)
                {
                    throw new NullReferenceException("One of the ids is null");
                }

                string[] lastSceneExport =
                    idController.ImportClassDataAndTransformIDs(scenePath, oldIDs, currentIDs,
                        ref foundScripts);
                
                Instance().Enqueue(() => { ImportAfterIDTransformationOnMainThread(rootPath, scenePath, foundScripts, lastSceneExport); });
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }
        }

        private void
            ImportAfterIDTransformationOnMainThread(string rootPath, string scenePath,
                List<FoundScript> foundScripts,
                string[] lastSceneExport)
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
                        .GroupBy(field => field.newClassModel.FullName)
                        .Select(group => group.First()).ToList();

                EditorUtility.DisplayDialog("Merging fields necessary",
                    "Could not merge all the fields to the class in the new project. You'll have to manually match old fields with the new fields",
                    "Open merge window");

                mergingWizard = MergingWizard.CreateWizard(scripts);

                mergingWizard.onComplete = (list) =>
                {
                    MergingWizardCompleted(foundScripts, list, rootPath, scenePath, lastSceneExport);
                };
            }
            else
            {
                SaveFoundScripts(rootPath, foundScripts);
                SaveFile(rootPath + "/" + Path.GetFileName(scenePath), lastSceneExport);
                calculationThread = null;
            }
        }

        private static void SaveFoundScripts(string rootPath, List<FoundScript> foundScripts)
        {
            string foundScriptsPath = rootPath + "/ImportExport/Exports/Found.json";
            File.WriteAllText(foundScriptsPath, JsonConvert.SerializeObject(foundScripts, Formatting.Indented));
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
//            GUIUtility.systemCopyBuffer = newScenePath;
            EditorUtility.DisplayDialog("Imported data", "The scene was exported to " + newScenePath, "Ok");
        }

        /// <summary>
        /// Change the fields after merging with the merging window
        /// </summary>
        /// <param name="scripts"></param>
        /// <param name="mergedFoundScripts"></param>
        /// <param name="rootPath"></param>
        /// <param name="scenePath"></param>
        /// <param name="linesToChange"></param>
        private void MergingWizardCompleted(List<FoundScript> originalFoundScripts,
            List<FoundScript> mergedFoundScripts, string rootPath,
            string scenePath,
            string[] linesToChange)
        {
            // Merge the MergeWindow changed FoundScripts with the originalFoundScripts
            for (var i = 0; i < originalFoundScripts.Count; i++)
            {
                FoundScript originalFoundScript = originalFoundScripts[i];
                FoundScript changedFoundScript = mergedFoundScripts.FirstOrDefault(script =>
                    script.oldClassModel.FullName == originalFoundScript.oldClassModel.FullName);
                if (changedFoundScript != null)
                {
                    originalFoundScripts[i] = changedFoundScript;
                }
            }

            string[] newSceneExport =
                fieldMappingController.ReplaceFieldsByMergeNodes(linesToChange, originalFoundScripts);

            Debug.Log(string.Join("\n", newSceneExport));

            SaveFoundScripts(rootPath, originalFoundScripts);
            SaveFile(rootPath + "/" + Path.GetFileName(scenePath), linesToChange);
            calculationThread = null;
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