#if UNITY_EDITOR

using importerexporter.controllers;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using importerexporter.models;
using importerexporter.utility;
using importerexporter.windows;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace importerexporter.views
{
    public class SceneView
    {
        private readonly Constants constants = Constants.Instance;
        private readonly IDController idController = IDController.Instance;
        private readonly FieldMappingController fieldMappingController = FieldMappingController.Instance;
        private readonly MigrationWindow mainThread= (MigrationWindow)MigrationWindow.Instance();
        
        private static MergingWizard mergingWizard;
        private Thread calculationThread;

        private static List<ClassModel> oldFileDatas;
        
        public void ImportClassDataAndScene()
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

                mainThread.Enqueue(() =>
                {
                    ImportAfterIDTransformationOnMainThread(rootPath, scenePath, foundScripts, lastSceneExport, oldIDs,
                            currentIDs);
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
                originalFoundScripts = originalFoundScripts.Merge(mergedFoundScripts);
            }

            fieldMappingController.ReplaceFieldsByMergeNodes(ref linesToChange, originalFoundScripts,
                ProjectPathUtility.getProjectPathFromFile(scenePath), rootPath, oldIDs, currentIDs);


            Debug.Log("Exported scene, Please press   Ctrl + R   to view it in the project tab. File:  " + rootPath +
                      "/" + Path.GetFileName(scenePath) + "");

            SaveFoundScripts(rootPath, originalFoundScripts);
            SaveFile(rootPath + "/" + Path.GetFileName(scenePath), linesToChange);
            calculationThread = null;


            AssetDatabase.Refresh();
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
    }
}
#endif