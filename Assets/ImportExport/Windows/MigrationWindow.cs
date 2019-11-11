#if UNITY_EDITOR

using importerexporter.controllers;
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
using YamlDotNet.RepresentationModel;

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
    public class MigrationWindow : MainThreadDispatcherEditorWindow
    {
        private readonly Constants constants = Constants.Instance;
        private readonly IDController idController = IDController.Instance;
        private readonly FieldMappingController fieldMappingController = FieldMappingController.Instance;


        private static List<ClassModel> oldFileDatas;

        private static MergingWizard mergingWizard;
        private Thread calculationThread;

        private string idExportPath;
        bool exportExists;


        private List<ClassModel> cachedLocalIds;


        [MenuItem("Window/Scene migration window")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow<MigrationWindow>("Migration Window");
        }

        protected void OnEnable()
        {
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

            if (GUILayout.Button("Convert prefab"))
            {
                CopyPrefabs();
            }

            EditorGUI.EndDisabledGroup();

            GUILayout.Label(exportExists
                ? "IDs found, ready to migrate."
                : "No IDs found, please export the current IDs.");
        }

        private void CopyPrefabs()
        {
            const string sceneFile =
                @"D:\UnityProjects\GITHUB\ImportingOldTestProject\Assets\Scenes\Prefab scene.unity";

            string originalAssetPath = ProjectPathUtility.getProjectPathFromFile(sceneFile);
            const string destinationAssetPath = @"D:\UnityProjects\GITHUB\SceneImportExporter\Assets\";

            ParsePrefab(sceneFile, originalAssetPath, destinationAssetPath);
        }

        private void ParsePrefab(string sceneFile, string originalAssetPath, string destinationAssetPath)
        {
            YamlStream stream = new YamlStream();
            stream.Load(new StringReader(File.ReadAllText(sceneFile)));
            List<YamlDocument> yamlPrefabs =
                stream.Documents.Where(document => document.GetName() == "PrefabInstance").ToList();

            List<PrefabModel> prefabs = PrefabController.Instance.ExportPrefabs(originalAssetPath);
            foreach (YamlDocument prefabInstance in yamlPrefabs)
            {
                YamlNode sourcePrefab = prefabInstance.RootNode.GetChildren()["PrefabInstance"]["m_SourcePrefab"];
                string guid = (string) sourcePrefab["guid"];


                PrefabModel currentPrefab = prefabs.First(prefab => prefab.Guid == guid);
                string[] parsedPrefab = File.ReadAllLines(currentPrefab.Path);

                string originalProjectPath = ProjectPathUtility.getProjectPathFromFile(sceneFile);
                string relativeExportLocation = @"\ImportExport\Exports\Export.json";
                List<ClassModel> oldIDs =
                    JsonConvert.DeserializeObject<List<ClassModel>>(
                        File.ReadAllText(originalProjectPath + relativeExportLocation));
                List<ClassModel> newIDs =
                    JsonConvert.DeserializeObject<List<ClassModel>>(
                        File.ReadAllText(destinationAssetPath + relativeExportLocation));
                List<FoundScript> foundScripts =
                    JsonConvert.DeserializeObject<List<FoundScript>>(
                        File.ReadAllText(destinationAssetPath + @"ImportExport\Exports\Found.json"));

                new Thread(() =>
                {
                    parsedPrefab = idController.TransformIDs(currentPrefab.Path, oldIDs, newIDs, ref foundScripts);
                    this.Enqueue(() =>
                    {
                        MergingWizard wizard = MergingWizard.CreateWizard(foundScripts
                            .Where(script => script.HasBeenMapped == FoundScript.MappedState.NotMapped).ToList());
                        wizard.onComplete = mergedFoundScripts =>
                        {
                            List<FoundScript> latestFoundScripts = MergeFoundScripts(foundScripts, mergedFoundScripts);

                            parsedPrefab = FieldMappingController.Instance.ReplaceFieldsByMergeNodes(parsedPrefab,
                                latestFoundScripts,
                                originalAssetPath, destinationAssetPath, oldIDs, newIDs);
                            WritePrefab(parsedPrefab, currentPrefab, destinationAssetPath);
                        };
                    });
                }).Start();
            }
        }

        private void WritePrefab(string[] parsedPrefab, PrefabModel currentPrefab, string destination)
        {
            string newPrefabPath = destination + Path.GetFileName(currentPrefab.MetaPath);
            if (File.Exists(newPrefabPath))
            {
                if (!EditorUtility.DisplayDialog("Prefab already exists",
                    "Prefab file already exists, overwrite? \r\n File : " + newPrefabPath, "Overwrite"))
                {
                    Debug.LogWarning(
                        "Could not write the prefab as the file already exists.\r\n File: " + newPrefabPath
                        );
                    return;
                }
            }

            File.Copy(currentPrefab.MetaPath, newPrefabPath, true);
            File.WriteAllText(destination + Path.GetFileName(currentPrefab.Path),
                string.Join("\r\n", parsedPrefab));
            Debug.Log("Wrote the prefab");
        }

        private void ExportCurrentClassData(string rootPath)
        {
            List<ClassModel> IDs = idController.ExportClassData(rootPath);

            var jsonSerializerSettings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                Formatting = Formatting.Indented
            };

            string jsonField = JsonConvert.SerializeObject(IDs, jsonSerializerSettings);

            if (!Directory.Exists(Path.GetDirectoryName(idExportPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(idExportPath));
            }

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

            EditorUtility.DisplayDialog("Please select the scene", "Please select the scene to migrate.",
                "Select the scene");
            string scenePath =
                EditorUtility.OpenFilePanel("Scene to import", Application.dataPath,
                    "unity"); //todo : check if this is in the current project
            if (scenePath.Length == 0)
            {
                Debug.LogWarning("No path was selected");
                return;
            }

            string IDPath = ProjectPathUtility.getProjectPathFromFile(scenePath) + @"\ImportExport\Exports\Export.json";

            if (!File.Exists(IDPath))
            {
                EditorUtility.DisplayDialog("Could not find old ID's",
                    "Could not find the ID's of the original project.  File does not exist : \r\n" + IDPath, "Ok");
                return;
            }

            List<ClassModel> oldIDs =
                JsonConvert.DeserializeObject<List<ClassModel>>(File.ReadAllText(IDPath));

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


        /// <summary>
        /// Make a copy of the scene file and change the GUIDs, fileIDs and if necessary the fields 
        /// </summary>
        /// <param name="rootPath"></param>
        /// <param name="oldIDs"></param>
        /// <param name="currentIDs"></param>
        /// <param name="scenePath"></param>
        /// <param name="foundScripts"></param>
        public void ImportTransformIDs(string rootPath, List<ClassModel> oldIDs, List<ClassModel> currentIDs,
            string scenePath,
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
                    idController.TransformIDs(scenePath, oldIDs, currentIDs,
                        ref foundScripts);

                Instance().Enqueue(() =>
                {
                    ImportAfterIDTransformationOnMainThread(rootPath, scenePath, foundScripts, lastSceneExport, oldIDs,
                            currentIDs)
                        ;
                });
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
                string[] lastSceneExport, List<ClassModel> oldIDs, List<ClassModel> currentIDs)
        {
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

                mergingWizard.onComplete = (userAuthorizedList) =>
                {
                    MergingWizardCompleted(foundScripts, rootPath, scenePath, lastSceneExport, oldIDs, currentIDs,
                        userAuthorizedList);
                };
            }
            else
            {
//                SaveFoundScripts(rootPath, foundScripts);
//                SaveFile(rootPath + "/" + Path.GetFileName(scenePath), lastSceneExport);
//                calculationThread = null;
                MergingWizardCompleted(foundScripts, rootPath, scenePath, lastSceneExport, oldIDs, currentIDs);
            }
        }

        /// <summary>
        /// Change the fields after merging with the merging window
        /// </summary>
        /// <param name="originalFoundScripts"></param>
        /// <param name="rootPath"></param>
        /// <param name="scenePath"></param>
        /// <param name="linesToChange"></param>
        /// <param name="mergedFoundScripts"></param>
        private void MergingWizardCompleted(List<FoundScript> originalFoundScripts, string rootPath,
            string scenePath,
            string[] linesToChange, List<ClassModel> oldIDs, List<ClassModel> currentIDs,
            List<FoundScript> mergedFoundScripts = null)
        {
            if (mergedFoundScripts != null)
            {
                originalFoundScripts = MergeFoundScripts(originalFoundScripts, mergedFoundScripts);
            }

            string[] newSceneExport =
                fieldMappingController.ReplaceFieldsByMergeNodes(linesToChange, originalFoundScripts,
                    ProjectPathUtility.getProjectPathFromFile(scenePath), rootPath, oldIDs, currentIDs);

            Debug.Log(string.Join("\n", newSceneExport));

            SaveFoundScripts(rootPath, originalFoundScripts);
            SaveFile(rootPath + "/" + Path.GetFileName(scenePath), linesToChange);
            calculationThread = null;


            AssetDatabase.Refresh();
        }

        private static List<FoundScript> MergeFoundScripts(List<FoundScript> originalFoundScripts,
            List<FoundScript> mergedFoundScripts)
        {
            if (originalFoundScripts == null || mergedFoundScripts == null)
            {
                throw new NullReferenceException("Could not merge foundScripts for null foundScript list");
            }

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

            return originalFoundScripts;
        }


        /// <summary>
        /// Write foundScripts to a file
        /// </summary>
        /// <param name="rootPath"></param>
        /// <param name="foundScripts"></param>
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

            if (!Directory.Exists(Path.GetDirectoryName(newScenePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(newScenePath));
            }

            File.WriteAllText(newScenePath, string.Join("\n", linesToWrite));
            EditorUtility.DisplayDialog("Imported data", "The scene was exported to " + newScenePath, "Ok");
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