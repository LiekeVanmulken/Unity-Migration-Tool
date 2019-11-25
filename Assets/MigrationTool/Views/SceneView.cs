#if UNITY_EDITOR

using migrationtool.controllers;
using System;
using System.IO;
using System.Linq;
using migrationtool.models;
using migrationtool.utility;
using migrationtool.windows;
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

        /// <summary>
        /// Migrate all scenes in a project at once at once
        /// </summary>
        /// <param name="projectToExportFromPath">The path of the project that needs to be migrated to the current project</param>
        public void MigrateAllScenes(string projectToExportFromPath = null)
        {
            MigrationWindow.DisplayProgressBar("Exporting scenes", "Exporting scenes", 0.2f);
            string rootPath = Application.dataPath;
            
            if (projectToExportFromPath == null)
            {
                projectToExportFromPath =
                    EditorUtility.OpenFolderPanel("Export all scenes in folder", rootPath, "");
            }

            if (string.IsNullOrEmpty(projectToExportFromPath))
            {
                Debug.Log("Copy prefabs aborted, no path given.");
                return;
            }

            ThreadUtil.RunThread(() =>
            {
                string[] sceneFiles =
                    Directory.GetFiles(projectToExportFromPath, "*.unity", SearchOption.AllDirectories);

                for (var i = 0; i < sceneFiles.Length; i++)
                {
                    string scene = sceneFiles[i];
                    MigrationWindow.DisplayProgressBar("Exporting scenes", "Exporting scene: " + scene,
                        i + 1 / sceneFiles.Length);

                    ThreadUtil.RunWaitThread(() => { MigrateScene(scene,rootPath); });
                }

                MigrationWindow.ClearProgressBar();
                Debug.Log("Migrated all scenes");
            });
        }

        /// <summary>
        /// Migrate a scene
        /// </summary>
        /// <param name="scenePath">The scene file to migrate</param>
        public void MigrateScene(string scenePath = null, string rootPath = null)
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

            if (rootPath == null)
            {
                rootPath = Application.dataPath;
            }

            string IDPath = ProjectPathUtility.getProjectPathFromFile(scenePath) + constants.RelativeExportPath;

            if (!File.Exists(IDPath))
            {
                EditorUtility.DisplayDialog("Could not find old ID's",
                    "Could not find the ID's of the original project.  File does not exist : \r\n" + IDPath, "Ok");
                return;
            }

            ThreadUtil.RunWaitMainThread(() =>
            {
                List<ClassModel> oldIDs =
                    Administration.Instance.oldIDsOverride ?? IDController.DeserializeIDs(IDPath);

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

                this.MigrateSceneIDs(rootPath, oldIDs, newIDs, scenePath, foundScripts);
            });
            Debug.Log("Exported scene : " + scenePath);
        }


        /// <summary>
        /// Make a copy of the scene file and change the GUIDs, fileIDs and if necessary the fields 
        /// </summary>
        /// <param name="rootPath"></param>
        /// <param name="oldIDs"></param>
        /// <param name="currentIDs"></param>
        /// <param name="scenePath"></param>
        /// <param name="foundScripts"></param>
        private void MigrateSceneIDs(string rootPath, List<ClassModel> oldIDs, List<ClassModel> currentIDs,
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
                        MigrateFields(rootPath, scenePath, foundScripts, lastSceneExport);
                    }
                );
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }
        }

        private void
            MigrateFields(string rootPath, string scenePath,
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

                MergeWizard mergeWizard = MergeWizard.CreateWizard(scripts);
                mergeWizard.onComplete = (userAuthorizedList) =>
                {
                    MergingWizardCompleted(foundScripts, rootPath, scenePath, lastSceneExport, userAuthorizedList);
                };
            }
            else
            {
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
                fieldMappingController.MigrateFields(scenePath, ref linesToChange, ref originalFoundScripts,
                    ProjectPathUtility.getProjectPathFromFile(scenePath), rootPath);

                string newScenePath = rootPath + scenePath.GetRelativeAssetPath();

                if (!Administration.Instance.OverWriteMode)
                {
                    newScenePath = ProjectPathUtility.AddTimestamp(newScenePath);
                }

                Debug.Log("Exported scene, View it in the project tab, file:  " + newScenePath);

                mappingView.SaveFoundScripts(rootPath, originalFoundScripts);
                SaveSceneFile(newScenePath, linesToChange);
            });
        }

        /// <summary>
        /// Saves the <param name="linesToWrite"/> to a new file at the <param name="scenePath"/>
        /// </summary>
        /// <param name="scenePath"></param>
        /// <param name="linesToWrite"></param>
        private void SaveSceneFile(string scenePath, string[] linesToWrite)
        {
            if (!Directory.Exists(Path.GetDirectoryName(scenePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(scenePath));
            }

            File.WriteAllText(scenePath, string.Join("\n", linesToWrite));
            MigrationWindow.Instance().Enqueue(() =>
            {
                AssetDatabase.Refresh();
                if (Administration.Instance.ShowInfoPopups)
                {
                    EditorUtility.DisplayDialog("Imported data",
                        "The scene was migrated to " + scenePath.GetRelativeAssetPath(), "Ok");
                }
            });
        }
    }
}
#endif