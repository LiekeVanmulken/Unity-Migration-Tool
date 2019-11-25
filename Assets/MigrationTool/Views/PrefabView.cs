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
            string destinationAssetPath, ref List<FoundScript> foundScripts) // todo : this will not use the classModel!!
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

                foundScripts = ParsePrefab(currentPrefab.Path, originalAssetPath, destinationAssetPath, prefabs, currentPrefab.Guid, foundScripts);
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
        public List<FoundScript> ParsePrefab(string prefabFile, string originalAssetPath, string destinationAssetPath,
            List<PrefabModel> prefabs,
            string prefabGuid, List<FoundScript> foundScripts)
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

            if (!File.Exists(originalProjectPath + constants.RelativeExportPath) ||
                !File.Exists(destinationAssetPath + constants.RelativeExportPath))
            {
                throw new NullReferenceException(
                    "Could not find one of the two Export.json files. Please export the IDs again in both projects ");
            }

            //Deserialize the old ID's
            List<ClassModel> oldIDs =
                Administration.Instance.oldIDsOverride ?? IDController.DeserializeIDs(originalProjectPath + constants.RelativeExportPath);

            //Deserialize the new ID's
            List<ClassModel> newIDs =
                IDController.DeserializeIDs(destinationAssetPath + constants.RelativeExportPath);


            //Deserialize the foundScripts
//            List<FoundScript> foundScripts = new List<FoundScript>();
//            if (File.Exists(destinationAssetPath + constants.RelativeFoundScriptPath))
//            {
//                foundScripts = MappingController.DeserializeMapping(destinationAssetPath + constants.RelativeFoundScriptPath);
//            }


            parsedPrefab = idController.TransformIDs(currentPrefab.Path, oldIDs, newIDs, ref foundScripts);

            var unmappedFoundScripts = foundScripts
                .Where(script => script.HasBeenMapped == FoundScript.MappedState.NotMapped).ToList();
            if (unmappedFoundScripts.Count == 0)
            {
                parsedPrefab = fieldMappingController.MigrateFields(prefabFile, ref parsedPrefab,
                    ref foundScripts,
                    originalAssetPath, destinationAssetPath);
                SavePrefabFile(parsedPrefab, currentPrefab, destinationAssetPath);
            }
            else
            {
                bool completed = false;
                MigrationWindow.Instance().Enqueue(() =>
                {
                    MergeWizard wizard = MergeWizard.CreateWizard(unmappedFoundScripts);
                    wizard.onComplete = mergedFoundScripts =>
                    {
                        foundScripts = foundScripts.Merge(mergedFoundScripts);
                        File.WriteAllText(destinationAssetPath + constants.RelativeFoundScriptPath,
                            JsonConvert.SerializeObject(foundScripts, constants.IndentJson));

                        ThreadUtil.RunThread(() =>
                        {
                            parsedPrefab = fieldMappingController.MigrateFields(prefabFile, ref parsedPrefab,
                                ref foundScripts, originalAssetPath, destinationAssetPath);
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

            return foundScripts;
        }

        public void MigrateAllPrefabs(string destinationProjectPath, string originalProjectPath = null)
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
            
            //Deserialize the foundScripts
            List<FoundScript> foundScripts = new List<FoundScript>();
            if (File.Exists(destinationProjectPath + constants.RelativeFoundScriptPath))
            {
                foundScripts = MappingController.DeserializeMapping(destinationProjectPath + constants.RelativeFoundScriptPath);
            }

            List<PrefabModel> prefabs = new PrefabController().ExportPrefabs(originalProjectPath);
            foreach (PrefabModel prefab in prefabs)
            {
                ThreadUtil.RunWaitThread(() =>
                    {
                        ParsePrefab(prefab.Path, originalProjectPath, destinationProjectPath, prefabs,
                            prefab.Guid, foundScripts);
                    }
                );
            }

            ThreadUtil.RunMainThread(() => { AssetDatabase.Refresh(); });
            Debug.Log("Migrated all prefabs");
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