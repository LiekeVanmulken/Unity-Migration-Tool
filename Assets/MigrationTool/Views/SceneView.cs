#if UNITY_EDITOR

using migrationtool.controllers;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using migrationtool.models;
using migrationtool.utility;
using migrationtool.windows;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace migrationtool.views
{
    public class SceneView
    {
        private Constants constants = Constants.Instance;

        private MappingView mappingView = new MappingView();

        private IDController idController = new IDController();
        private FieldMappingController fieldMappingController = new FieldMappingController();


        private MergeWizard mergeWizard;
//        private Thread calculationThread;

        private static List<ClassModel> oldFileDatas;


        public void MigrateAllScenes()
        {
            string rootPath = Application.dataPath;
            string selectedAssetPath =
                EditorUtility.OpenFolderPanel("Export all scenes in folder", rootPath, "");

            if (string.IsNullOrEmpty(selectedAssetPath))
            {
                Debug.Log("Copy prefabs aborted, no path given.");
                return;
            }

            string[] sceneFiles = Directory.GetFiles(selectedAssetPath, "*.unity", SearchOption.AllDirectories);

            foreach (string scene in sceneFiles)
            {
                MigrateScene(scene);
            }

            Debug.Log("Migrated all scenes");
        }

        public void MigrateScene(string scenePath = null)
        {
            if (scenePath == null)
            {
                scenePath =
                    EditorUtility.OpenFilePanel("Scene to import", Application.dataPath,
                        "unity"); //todo : check if this is in the current project
                if (scenePath.Length == 0)
                {
                    Debug.LogWarning("No path was selected");
                    return;
                }
            }

            string IDPath = ProjectPathUtility.getProjectPathFromFile(scenePath) + constants.RelativeExportPath;

            if (!File.Exists(IDPath))
            {
                EditorUtility.DisplayDialog("Could not find old ID's",
                    "Could not find the ID's of the original project.  File does not exist : \r\n" + IDPath, "Ok");
                return;
            }

            List<ClassModel> oldIDs =
                Administration.Instance.oldIDsOverride ?? IDController.DeserializeIDs(IDPath);

            string rootPath = Application.dataPath;
            string newIDsPath = rootPath + constants.RelativeExportPath;

            List<ClassModel> newIDs = File.Exists(newIDsPath)
                ? IDController.DeserializeIDs(newIDsPath)
                : idController.ExportClassData(rootPath);


            List<FoundScript> foundScripts = new List<FoundScript>();
            string foundScriptsPath = rootPath + constants.RelativeFoundScriptPath;
            if (File.Exists(foundScriptsPath))
            {
                foundScripts = MappingController.DeserializeMapping(foundScriptsPath);
            }

            ThreadUtil.RunThread(() => { this.ImportTransformIDs(rootPath, oldIDs, newIDs, scenePath, foundScripts); });
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
                if (oldIDs == null || currentIDs == null)
                {
                    throw new NullReferenceException("One of the ids is null");
                }

                string[] lastSceneExport =
                    idController.TransformIDs(scenePath, oldIDs, currentIDs,
                        ref foundScripts);

                MigrationWindow.Instance().Enqueue(() =>
                {
                    ImportAfterIDTransformationOnMainThread(rootPath, scenePath, foundScripts, lastSceneExport);
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
                string[] lastSceneExport)
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

                mergeWizard = MergeWizard.CreateWizard(scripts);

                mergeWizard.onComplete = (userAuthorizedList) =>
                {
                    MergingWizardCompleted(foundScripts, rootPath, scenePath, lastSceneExport, userAuthorizedList);
                };
            }
            else
            {
//                SaveFoundScripts(rootPath, foundScripts);
//                SaveFile(rootPath + "/" + Path.GetFileName(scenePath), lastSceneExport);
//                calculationThread = null;
                MergingWizardCompleted(foundScripts, rootPath, scenePath, lastSceneExport);
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
            string[] linesToChange,
            List<FoundScript> mergedFoundScripts = null)
        {
            if (mergedFoundScripts != null)
            {
                originalFoundScripts = originalFoundScripts.Merge(mergedFoundScripts);
            }

            ThreadUtil.RunThread(() =>
            {
                fieldMappingController.MigrateFields(scenePath, ref linesToChange, originalFoundScripts,
                    ProjectPathUtility.getProjectPathFromFile(scenePath), rootPath);

                string newScenePath = rootPath + scenePath.GetRelativeAssetPath();

                if (!Administration.Instance.OverwriteFiles)
                {
                    newScenePath = ProjectPathUtility.AddTimestamp(newScenePath);
                }

                Debug.Log("Exported scene, View it in the project tab, file:  " + newScenePath);

                mappingView.SaveFoundScripts(rootPath, originalFoundScripts);
                SaveFile(newScenePath, linesToChange);
            });
        }

        private string GetRelativePath(string rootPath, string scene)
        {
            return scene.Substring(ProjectPathUtility.getProjectPathFromFile(scene).Length);
        }

        /// <summary>
        /// Saves the <param name="linesToWrite"/> to a new file at the <param name="scenePath"/>
        /// </summary>
        /// <param name="scenePath"></param>
        /// <param name="linesToWrite"></param>
        private void SaveFile(string scenePath, string[] linesToWrite)
        {
            if (!Directory.Exists(Path.GetDirectoryName(scenePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(scenePath));
            }

            File.WriteAllText(scenePath, string.Join("\n", linesToWrite));
            MigrationWindow.Instance().Enqueue(() =>
            {
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Imported data",
                    "The scene was migrated to " + scenePath.GetRelativeAssetPath(), "Ok");
            });
        }
    }
}
#endif