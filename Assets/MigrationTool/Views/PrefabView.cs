#if UNITY_EDITOR

using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using migrationtool.controllers;
using migrationtool.models;
using migrationtool.utility;
using migrationtool.windows;
using Newtonsoft.Json;
using UnityEngine;
using YamlDotNet.RepresentationModel;

namespace migrationtool.views
{
    /// <summary>
    /// Handles the UI logic of the prefab conversion
    /// </summary>
    public class PrefabView
    {
        private readonly Constants constants = Constants.Instance;

        private readonly IDController idController = new IDController();
        private readonly PrefabController prefabController = new PrefabController();
        private readonly FieldMappingController fieldMappingController = new FieldMappingController();

        /// <summary>
        /// Find all prefabs in the scene and parse the prefabs.
        /// Change the guid's and fileID's on scripts and migrate the fields 
        /// </summary>
        /// <param name="sceneFile"></param>
        /// <param name="originalAssetPath"></param>
        /// <param name="destinationAssetPath"></param>
        /// <param name="oldIDs">Needs to be added because the oldIDs can be overriden</param>
        public void ParsePrefabsInAScene(string sceneFile, string originalAssetPath,
            string destinationAssetPath,
            ref List<ScriptMapping> scriptMappings)
        {
            YamlStream stream = new YamlStream();
            string[] lines = File.ReadAllLines(sceneFile).PrepareSceneForYaml();
            stream.Load(new StringReader(string.Join("\r\n", lines)));
            List<YamlDocument> yamlPrefabs =
                stream.Documents.Where(document => document.GetName() == "PrefabInstance").ToList();

            List<PrefabModel> prefabs = prefabController.ExportPrefabs(originalAssetPath);
            List<string> prefabGuids = yamlPrefabs.Select(document =>
                    (string) document.RootNode.GetChildren()["PrefabInstance"]["m_SourcePrefab"]["guid"])
                .Distinct()
                .ToList();

            foreach (string prefabGuid in prefabGuids)
            {
                PrefabModel currentPrefab = prefabs.FirstOrDefault(model => model.Guid == prefabGuid);
                if (currentPrefab == null)
                {
                    Debug.LogError("Find references to prefab but could not find prefab for guid : " + prefabGuid);
                    continue;
                }

                scriptMappings = ParsePrefab(currentPrefab.Path, originalAssetPath, destinationAssetPath, prefabs,
                    currentPrefab.Guid, scriptMappings);
            }
        }

        /// <summary>
        /// Parse the prefab.
        /// Change the guid's and fileID's on scripts and port the fields 
        /// </summary>
        /// <param name="prefabFile"></param>
        /// <param name="originalAssetPath"></param>
        /// <param name="destinationAssetPath"></param>
        /// <param name="prefabs"></param>
        /// <param name="prefabGuid"></param>
        /// <exception cref="FormatException"></exception>
        /// <exception cref="NullReferenceException"></exception>
        public List<ScriptMapping> ParsePrefab(string prefabFile, string originalAssetPath, string destinationAssetPath,
            List<PrefabModel> prefabs,
            string prefabGuid, List<ScriptMapping> scriptMappings)
        {
            if (!prefabFile.EndsWith(".prefab"))
            {
                throw new FormatException("Could not parse prefab, not of type prefab, file : " + prefabFile);
            }

            PrefabModel currentPrefab = prefabs.FirstOrDefault(prefab => prefab.Guid == prefabGuid);
            if (currentPrefab == null)
            {
            }

            string[] parsedPrefab = File.ReadAllLines(currentPrefab.Path);


            string originalProjectPath = ProjectPathUtility.getProjectPathFromFile(prefabFile);

//            if ((!File.Exists(originalProjectPath + constants.RelativeExportPath) ||
//                !File.Exists(destinationAssetPath + constants.RelativeExportPath)) && 
//                (
//                    Administration.Instance.oldIDsOverride == null 
//                    && 
//                    Administration.Instance.newIDsOverride == null)
//                )
//            {
//                throw new NullReferenceException(
//                    "Could not find one of the two Export.json files. Please export the IDs again in both projects ");
//            }

            //Deserialize the old ID's
            List<ClassModel> oldIDs =
                Administration.Instance.oldIDsOverride ??
                IDController.DeserializeIDs(originalProjectPath + constants.RelativeExportPath);
            if (oldIDs == null)
            {
                throw new NullReferenceException("Old IDs not set");
            }

            //Deserialize the new ID's
            List<ClassModel> newIDs =
                Administration.Instance.newIDsOverride??
                IDController.DeserializeIDs(destinationAssetPath + constants.RelativeExportPath);
            if (newIDs == null)
            {
                throw new NullReferenceException("New IDs not set");
            }

            parsedPrefab = idController.TransformIDs(currentPrefab.Path, oldIDs, newIDs, ref scriptMappings);

            var unmappedScriptMappings = scriptMappings
                .Where(script => script.HasBeenMapped == ScriptMapping.MappedState.NotMapped).ToList();
            if (unmappedScriptMappings.Count == 0)
            {
                parsedPrefab = fieldMappingController.MigrateFields(prefabFile, ref parsedPrefab,
                    ref scriptMappings,
                    originalAssetPath, destinationAssetPath);
                SavePrefabFile(parsedPrefab, currentPrefab, destinationAssetPath);
            }
            else
            {
                bool completed = false;
                ThreadUtil.RunMainThread(() =>
                {
                    MergeWizard wizard = MergeWizard.CreateWizard(unmappedScriptMappings);
                    wizard.onComplete = mergedScriptMappings =>
                    {
                        scriptMappings = scriptMappings.Merge(mergedScriptMappings);
                        File.WriteAllText(destinationAssetPath + constants.RelativeScriptMappingPath,
                            JsonConvert.SerializeObject(scriptMappings, constants.IndentJson));

                        ThreadUtil.RunThread(() =>
                        {
                            parsedPrefab = fieldMappingController.MigrateFields(prefabFile, ref parsedPrefab,
                                ref scriptMappings, originalAssetPath, destinationAssetPath);
                            SavePrefabFile(parsedPrefab, currentPrefab, destinationAssetPath);
                        });
                        completed = true;
                    };
                });

                while (!completed)
                {
                    Thread.Sleep(constants.THREAD_WAIT_TIME);
                }
            }

            return scriptMappings;
        }

        /// <summary>
        /// Migrate all prefabs
        /// </summary>
        /// <param name="destinationProjectPath"></param>
        /// <param name="originalProjectPath"></param>
        /// <param name="onComplete"></param>
        public void MigrateAllPrefabs(string destinationProjectPath, string originalProjectPath = null,
            Action onComplete = null, List<ScriptMapping> scriptMappings = null)
        {
            if (originalProjectPath == null)
            {
                ThreadUtil.RunWaitMainThread(() =>
                    {
                        originalProjectPath =
                            EditorUtility.OpenFolderPanel("Export all prefabs in folder", destinationProjectPath, "");
                    }
                );
            }

            if (string.IsNullOrEmpty(originalProjectPath))
            {
                Debug.Log("Copy prefabs aborted, no path given.");
                return;
            }

            //Deserialize the ScriptMappings
            if (scriptMappings == null && File.Exists(destinationProjectPath + constants.RelativeScriptMappingPath))
            {
                scriptMappings =
                    MappingController.DeserializeMapping(destinationProjectPath + constants.RelativeScriptMappingPath);
            }

            List<PrefabModel> prefabs = prefabController.ExportPrefabs(originalProjectPath + "/Assets");
            for (var i = 0; i < prefabs.Count; i++)
            {
                PrefabModel prefab = prefabs[i];
                MigrationWindow.DisplayProgressBar("Migrating prefab (" + (i+1) + "/" + prefabs.Count + ")", info: "Migrating prefab: " + prefab.Path.Substring(originalProjectPath.Length), progress: (float)(i+1)/prefabs.Count);
                ThreadUtil.RunWaitThread(() =>
                    {
                        ParsePrefab(prefab.Path, originalProjectPath, destinationProjectPath, prefabs,
                            prefab.Guid, scriptMappings);
                    }
                );
            }

            MigrationWindow.ClearProgressBar();

            ThreadUtil.RunMainThread(() => { AssetDatabase.Refresh(); });
            Debug.Log("Migrated all prefabs");
            onComplete?.Invoke();
        }


        /// <summary>
        /// Write the prefab to a new file
        /// </summary>
        /// <param name="parsedPrefab"></param>
        /// <param name="currentPrefab"></param>
        /// <param name="destination"></param>
        public void SavePrefabFile(string[] parsedPrefab, PrefabModel currentPrefab, string destination)
        {
//            string newPrefabMetaPath = destination + @"\" + Path.GetFileName(currentPrefab.MetaPath);
            string newPrefabMetaPath = destination + currentPrefab.MetaPath.GetRelativeAssetPath();
            if (!Administration.Instance.OverWriteMode)
            {
                newPrefabMetaPath = ProjectPathUtility.AddTimestamp(newPrefabMetaPath);
            }

            if (!Directory.Exists(Path.GetDirectoryName(newPrefabMetaPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(newPrefabMetaPath));
            }

            // For when copying to the same project
            if (currentPrefab.MetaPath != newPrefabMetaPath)
            {
                File.Copy(currentPrefab.MetaPath, newPrefabMetaPath, true);
            }


            string newPrefabPath = newPrefabMetaPath.Substring(0, newPrefabMetaPath.Length - 5);
            if (!Directory.Exists(Path.GetDirectoryName(newPrefabPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(newPrefabPath));
            }


            File.WriteAllText(newPrefabPath,
                string.Join("\r\n", parsedPrefab));
            Debug.Log("Migrated prefab to : " + newPrefabPath);
        }
    }
}
#endif