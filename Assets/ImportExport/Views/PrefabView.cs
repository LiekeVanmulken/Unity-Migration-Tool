using System;
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using importerexporter.controllers;
using importerexporter.models;
using importerexporter.utility;
using importerexporter.windows;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using YamlDotNet.RepresentationModel;

namespace importerexporter.views
{
    /// <summary>
    /// Handles the UI logic of the prefab conversion
    /// </summary>
    public class PrefabView
    {
        private readonly IDController idController = new IDController();
        private readonly PrefabController prefabController = new PrefabController();
        private readonly FieldMappingController fieldMappingController = new FieldMappingController();

        public void ParsePrefabsInAScene(string sceneFile, string originalAssetPath, string destinationAssetPath)
        {
            YamlStream stream = new YamlStream();
            string[] lines = File.ReadAllLines(sceneFile).PrepareSceneForYaml();
            stream.Load(new StringReader(string.Join("\r\n",lines)));
            List<YamlDocument> yamlPrefabs =
                stream.Documents.Where(document => document.GetName() == "PrefabInstance").ToList();

            List<PrefabModel> prefabs = prefabController.ExportPrefabs(originalAssetPath);
            List<string> prefabGuids = yamlPrefabs.Select(document =>
                    (string) document.RootNode.GetChildren()["PrefabInstance"]["m_SourcePrefab"]["guid"]).Distinct()
                .ToList();

            foreach (string prefabGuid in prefabGuids)
            {
                ParsePrefab(sceneFile, originalAssetPath, destinationAssetPath, prefabs, prefabGuid);
            }
        }

        private void ParsePrefab(string sceneFile, string originalAssetPath, string destinationAssetPath,
            List<PrefabModel> prefabs,
            string prefabGuid)
        {
            PrefabModel currentPrefab = prefabs.First(prefab => prefab.Guid == prefabGuid);
            string[] parsedPrefab = File.ReadAllLines(currentPrefab.Path);


            string originalProjectPath = ProjectPathUtility.getProjectPathFromFile(sceneFile);
            string relativeExportLocation = @"\ImportExport\Exports\Export.json";

            if (!File.Exists(originalProjectPath + relativeExportLocation) ||
                !File.Exists(destinationAssetPath + relativeExportLocation))
            {
                throw new NullReferenceException(
                    "Could not find one of the two Export.json files. Please export the IDs again in both projects ");
            }

            //Deserialize the old ID's
            List<ClassModel> oldIDs =
                JsonConvert.DeserializeObject<List<ClassModel>>(
                    File.ReadAllText(originalProjectPath + relativeExportLocation));
            
            //Deserialize the new ID's
            List<ClassModel> newIDs =
                JsonConvert.DeserializeObject<List<ClassModel>>(
                    File.ReadAllText(destinationAssetPath + relativeExportLocation));


            //Deserialize the foundScripts
            List<FoundScript> foundScripts = new List<FoundScript>();
            if (File.Exists(destinationAssetPath + @"\ImportExport\Exports\Found.json"))
            {
                JsonConvert.DeserializeObject<List<FoundScript>>(
                    File.ReadAllText(destinationAssetPath + @"\ImportExport\Exports\Found.json"));
            }

            new Thread(() =>
            {
                parsedPrefab = idController.TransformIDs(currentPrefab.Path, oldIDs, newIDs, ref foundScripts);
                MigrationWindow.Instance().Enqueue(() =>
                {
                    MergingWizard wizard = MergingWizard.CreateWizard(foundScripts
                        .Where(script => script.HasBeenMapped == FoundScript.MappedState.NotMapped).ToList());
                    wizard.onComplete = mergedFoundScripts =>
                    {
                        List<FoundScript> latestFoundScripts = foundScripts.Merge(mergedFoundScripts);

                        parsedPrefab = fieldMappingController.MigrateFields(sceneFile, ref parsedPrefab,
                            latestFoundScripts,
                            originalAssetPath, destinationAssetPath);
                        WritePrefab(parsedPrefab, currentPrefab, destinationAssetPath);
                    };
                });
            }).Start();
        }

        private void WritePrefab(string[] parsedPrefab, PrefabModel currentPrefab, string destination)
        {
            string newPrefabMetaPath = destination + @"\" + Path.GetFileName(currentPrefab.MetaPath);
            if (File.Exists(newPrefabMetaPath))
            {
                if (!EditorUtility.DisplayDialog("Prefab already exists",
                    "Prefab file already exists, overwrite? \r\n File : " + newPrefabMetaPath, "Overwrite"))
                {
                    Debug.LogWarning(
                        "Could not write the prefab as the file already exists.\r\n File: " +
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