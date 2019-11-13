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
        public void ParsePrefabsInAScene(string sceneFile, string originalAssetPath, string destinationAssetPath)
        {
            YamlStream stream = new YamlStream();
            string[] lines = File.ReadAllLines(sceneFile).PrepareSceneForYaml();
            stream.Load(new StringReader(string.Join("\r\n", lines)));
            List<YamlDocument> yamlPrefabs =
                stream.Documents.Where(document => document.GetName() == "PrefabInstance").ToList();

            List<PrefabModel> prefabs = prefabController.ExportPrefabs(originalAssetPath);
            List<string> prefabGuids = yamlPrefabs.Select(document =>
                    (string) document.RootNode.GetChildren()["PrefabInstance"]["m_SourcePrefab"]["guid"]).Distinct()
                .ToList();

            foreach (string prefabGuid in prefabGuids)
            {
                PrefabModel currentPrefab = prefabs.First(model => model.Guid == prefabGuid);
                ParsePrefab(currentPrefab.Path, originalAssetPath, destinationAssetPath, prefabs, currentPrefab.Guid);
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
        private void ParsePrefab(string prefabFile, string originalAssetPath, string destinationAssetPath,
            List<PrefabModel> prefabs,
            string prefabGuid)
        {
            if (!prefabFile.EndsWith(".prefab"))
            {
                throw new FormatException("Could not parse prefab, not of type prefab, file : " + prefabFile);
            }

            PrefabModel currentPrefab = prefabs.First(prefab => prefab.Guid == prefabGuid);
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
                JsonConvert.DeserializeObject<List<ClassModel>>(
                    File.ReadAllText(originalProjectPath + constants.RelativeExportPath));

            //Deserialize the new ID's
            List<ClassModel> newIDs =
                JsonConvert.DeserializeObject<List<ClassModel>>(
                    File.ReadAllText(destinationAssetPath + constants.RelativeExportPath));


            //Deserialize the foundScripts
            List<FoundScript> foundScripts = new List<FoundScript>();
            if (File.Exists(destinationAssetPath + constants.RelativeFoundScriptPath))
            {
                foundScripts = JsonConvert.DeserializeObject<List<FoundScript>>(
                    File.ReadAllText(destinationAssetPath + constants.RelativeFoundScriptPath));
            }

            bool completed = false;

            parsedPrefab = idController.TransformIDs(currentPrefab.Path, oldIDs, newIDs, ref foundScripts);

            var unmappedFoundScripts = foundScripts
                .Where(script => script.HasBeenMapped == FoundScript.MappedState.NotMapped).ToList();
            if (unmappedFoundScripts.Count == 0)
            {
                parsedPrefab = fieldMappingController.MigrateFields(prefabFile, ref parsedPrefab,
                    foundScripts,
                    originalAssetPath, destinationAssetPath);
                WritePrefab(parsedPrefab, currentPrefab, destinationAssetPath);
            }
            else
            {
                MigrationWindow.Instance().Enqueue(() =>
                {
                    MergingWizard wizard = MergingWizard.CreateWizard(unmappedFoundScripts);
                    wizard.onComplete = mergedFoundScripts =>
                    {
                        foundScripts = foundScripts.Merge(mergedFoundScripts);
                        File.WriteAllText(destinationAssetPath + constants.RelativeFoundScriptPath,
                            JsonConvert.SerializeObject(foundScripts, Formatting.Indented));

                        new Thread(() =>
                        {
                            parsedPrefab = fieldMappingController.MigrateFields(prefabFile, ref parsedPrefab,
                                foundScripts,
                                originalAssetPath, destinationAssetPath);
                            WritePrefab(parsedPrefab, currentPrefab, destinationAssetPath);
                            completed = true;
                        }).Start();
                    };
                });

                while (!completed)
                {
                    Thread.Sleep(500);
                }
            }
        }


        /// <summary>
        /// Write the prefab to a new file
        /// </summary>
        /// <param name="parsedPrefab"></param>
        /// <param name="currentPrefab"></param>
        /// <param name="destination"></param>
        private void WritePrefab(string[] parsedPrefab, PrefabModel currentPrefab, string destination)
        {
            string newPrefabMetaPath = destination + @"\" + Path.GetFileName(currentPrefab.MetaPath);
            if (File.Exists(newPrefabMetaPath))
            {
                bool completed = false;
                bool shouldOverwrite = false;
                MigrationWindow.Instance().Enqueue(() =>
                {
                    shouldOverwrite = EditorUtility.DisplayDialog("Prefab already exists",
                        "Prefab file already exists, overwrite? \r\n File : " + newPrefabMetaPath, "Overwrite",
                        "Ignore");
                    completed = true;
                });
                while (!completed)
                {
                    Thread.Sleep(500);
                }

                if (!shouldOverwrite)
                {
                    Debug.LogWarning(
                        "User chose not to overwrite the prefab as the file already exists.\r\n File: " +
                        newPrefabMetaPath.Replace(".meta", "")
                    );
                    return;
                }
            }

            File.Copy(currentPrefab.MetaPath, newPrefabMetaPath, true);

            string newPrefabPath = destination + @"\" + Path.GetFileName(currentPrefab.Path);

            File.WriteAllText(newPrefabPath,
                string.Join("\r\n", parsedPrefab));
            Debug.Log("Written the prefab to : " + newPrefabPath);
        }
    }
}
#endif