#if UNITY_EDITOR || UNITY_EDITOR_BETA

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
        /// <param name="onComplete">Runs after all scenes have been exported</param>
        public void MigrateAllScenes(string projectToExportFromPath = null, Action onComplete = null)
        {
            MigrationWindow.DisplayProgressBar("Migrating scenes", "Migrating scenes", 0.2f);

            if (projectToExportFromPath == null)
            {
                projectToExportFromPath =
                    EditorUtility.OpenFolderPanel("Export all scenes in folder", constants.RootDirectory, "");
            }

            if (string.IsNullOrEmpty(projectToExportFromPath))
            {
                Debug.Log("Migrating all scenes aborted, no path given.");
                MigrationWindow.ClearProgressBar();
                return;
            }

            ThreadUtility.RunTask(() =>
            {
                string[] sceneFiles =
                    Directory.GetFiles(projectToExportFromPath, "*.unity", SearchOption.AllDirectories);

                for (var i = 0; i < sceneFiles.Length; i++)
                {
                    string scene = sceneFiles[i];
                    MigrationWindow.DisplayProgressBar("Migrating scenes (" + (i + 1) + "/" + sceneFiles.Length + ")",
                        "Migrating scene: " + scene.Substring(projectToExportFromPath.Length),
                        (float) (i + 1) / sceneFiles.Length);

                    ThreadUtility.RunWaitTask(() => { MigrateScene(scene, constants.RootDirectory); });
                    GC.Collect();
                }

                MigrationWindow.ClearProgressBar();
                Debug.Log("Migrated all scenes");

                onComplete?.Invoke();
            });
        }

        /// <summary>
        /// Migrate a scene
        /// </summary>
        /// <param name="scenePath">The scene file to migrate</param>
        public void MigrateScene(string scenePath = null, string rootPath = null)
        {
            try
            {
                if (scenePath == null)
                {
                    ThreadUtility.RunWaitMainTask(() =>
                    {
                        scenePath =
                            EditorUtility.OpenFilePanel("Scene to import", constants.RootDirectory,
                                "unity");
                    });
                    if (scenePath.Length == 0)
                    {
                        Debug.LogWarning("No path was selected");
                        return;
                    }
                }

                if (rootPath == null)
                {
                    rootPath = constants.RootDirectory;
                }

                Debug.Log("Started migration of scene: " + scenePath);
                if (Utility.IsBinaryFile(scenePath))
                {
                    Debug.LogError("Could not parse file, since it's a binary file. Scene file: " + scenePath);
                    return;
                }

                string IDPath = ProjectPathUtility.getProjectPathFromFile(scenePath) + constants.RelativeExportPath;

                if (!File.Exists(IDPath))
                {
                    ThreadUtility.RunWaitMainTask(() =>
                    {
                        EditorUtility.DisplayDialog("Could not find old ID's",
                            "Could not find the ID's of the original project.  File does not exist : \r\n" + IDPath,
                            "Ok");
                    });
                    return;
                }

                List<ClassModel> oldIDs =
                    Administration.Instance.oldIDsOverride ?? IDController.DeserializeIDs(IDPath);

                string newIDsPath = rootPath + constants.RelativeExportPath;

                List<ClassModel> newIDs;
                if (Administration.Instance.newIDsOverride == null)
                {
                    newIDs = File.Exists(newIDsPath)
                        ? IDController.DeserializeIDs(newIDsPath)
                        : idController.ExportClassData(rootPath);
                }
                else
                {
                    newIDs = Administration.Instance.newIDsOverride;
                }

                List<ScriptMapping> scriptMappings = new List<ScriptMapping>();
                string scriptMappingsPath = rootPath + constants.RelativeScriptMappingPath;
                if (Administration.Instance.ScriptMappingsOverride != null)
                {
                    scriptMappings = Administration.Instance.ScriptMappingsOverride;
                }
                else if (File.Exists(scriptMappingsPath))
                {
                    scriptMappings = MappingController.DeserializeMapping(scriptMappingsPath);
                }

                this.MigrateSceneIDs(rootPath, oldIDs, newIDs, scenePath, scriptMappings);
                Debug.Log("Migrated scene : " + scenePath);
            }
            catch (Exception e)
            {
                Debug.LogError("Could not migrate scene: " + scenePath + "\r\nException: " + e);
            }
        }


        /// <summary>
        /// Make a copy of the scene file and change the GUIDs, fileIDs and if necessary the fields 
        /// </summary>
        /// <param name="rootPath"></param>
        /// <param name="oldIDs"></param>
        /// <param name="currentIDs"></param>
        /// <param name="scenePath"></param>
        /// <param name="scriptMappings"></param>
        private void MigrateSceneIDs(string rootPath, List<ClassModel> oldIDs, List<ClassModel> currentIDs,
            string scenePath,
            List<ScriptMapping> scriptMappings)
        {
            try
            {
                if (oldIDs == null || currentIDs == null)
                {
                    throw new NullReferenceException("One of the ids is null");
                }

                string[] lastSceneExport =
                    idController.TransformIDs(scenePath, oldIDs, currentIDs,
                        ref scriptMappings);

                ThreadUtility.RunMainTask(() => { MigrateFields(rootPath, scenePath, scriptMappings, lastSceneExport); }
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
                List<ScriptMapping> scriptMappings,
                string[] lastSceneExport)
        {
            foreach (ScriptMapping script in scriptMappings)
            {
                if (script.HasBeenMapped == ScriptMapping.MappedState.NotChecked)
                {
                    throw new NotImplementedException("Script has not been checked for mapping");
                }
            }


            ScriptMapping[] unmappedScripts = scriptMappings
                .Where(field => field.HasBeenMapped == ScriptMapping.MappedState.NotMapped).ToArray();
            if (unmappedScripts.Length > 0)
            {
                // Remove duplicate scripts
                List<ScriptMapping> scripts =
                    unmappedScripts
                        .GroupBy(field => field.newClassModel.FullName)
                        .Select(group => group.First()).ToList();

                EditorUtility.DisplayDialog("Merging fields necessary",
                    "Could not merge all the fields to the class in the new project. You'll have to manually match old fields with the new fields",
                    "Open merge window");

                MergeWizard mergeWizard = MergeWizard.CreateWizard(scripts);
                mergeWizard.onComplete = (userAuthorizedList) =>
                {
                    MergingWizardCompleted(scriptMappings, rootPath, scenePath, lastSceneExport,
                        userAuthorizedList);
                };
            }
            else
            {
                MergingWizardCompleted(scriptMappings, rootPath, scenePath, lastSceneExport);
            }
        }

        /// <summary>
        /// Change the fields after merging with the merging window
        /// </summary>
        /// <param name="originalScriptMappings"></param>
        /// <param name="rootPath"></param>
        /// <param name="scenePath"></param>
        /// <param name="linesToChange"></param>
        /// <param name="mergedScriptMappings"></param>
        private void MergingWizardCompleted(List<ScriptMapping> originalScriptMappings, string rootPath,
            string scenePath,
            string[] linesToChange,
            List<ScriptMapping> mergedScriptMappings = null)
        {
            if (mergedScriptMappings != null)
            {
                originalScriptMappings = originalScriptMappings.Merge(mergedScriptMappings);
            }

            ThreadUtility.RunTask(() =>
            {
                fieldMappingController.MigrateFields(scenePath, ref linesToChange, ref originalScriptMappings,
                    ProjectPathUtility.getProjectPathFromFile(scenePath), rootPath);

                string newScenePath = rootPath + scenePath.GetRelativeAssetPath();

                if (!Administration.Instance.OverWriteMode)
                {
                    newScenePath = ProjectPathUtility.AddTimestamp(newScenePath);
                }

                mappingView.SaveScriptMappings(rootPath, originalScriptMappings);
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
            ThreadUtility.RunWaitMainTask(() =>
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