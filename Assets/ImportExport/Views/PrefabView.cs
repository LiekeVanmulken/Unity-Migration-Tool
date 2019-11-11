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

        public void ParsePrefab(string sceneFile, string originalAssetPath, string destinationAssetPath)
        {
            YamlStream stream = new YamlStream();
            stream.Load(new StringReader(File.ReadAllText(sceneFile)));
            List<YamlDocument> yamlPrefabs =
                stream.Documents.Where(document => document.GetName() == "PrefabInstance").ToList();

            List<PrefabModel> prefabs = prefabController.ExportPrefabs(originalAssetPath);
            List<string> prefabGuids = yamlPrefabs.Select(document =>
                    (string) document.RootNode.GetChildren()["PrefabInstance"]["m_SourcePrefab"]["guid"]).Distinct()
                .ToList();

//            foreach (YamlDocument prefabInstance in yamlPrefabs)
            foreach (string prefabGuid in prefabGuids)
            {
//                YamlNode sourcePrefab = prefabInstance.RootNode.GetChildren()["PrefabInstance"]["m_SourcePrefab"];
//                string guid = (string) sourcePrefab["guid"];


                PrefabModel currentPrefab = prefabs.First(prefab => prefab.Guid == prefabGuid);
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
                    MigrationWindow.Instance().Enqueue(() =>
                    {
                        MergingWizard wizard = MergingWizard.CreateWizard(foundScripts
                            .Where(script => script.HasBeenMapped == FoundScript.MappedState.NotMapped).ToList());
                        wizard.onComplete = mergedFoundScripts =>
                        {
                            List<FoundScript> latestFoundScripts = foundScripts.Merge(mergedFoundScripts);

                            parsedPrefab = fieldMappingController.MigrateFields(ref parsedPrefab,
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
            string newPrefabMetaPath = destination + Path.GetFileName(currentPrefab.MetaPath);
            if (File.Exists(newPrefabMetaPath))
            {
                if (!EditorUtility.DisplayDialog("Prefab already exists",
                    "Prefab file already exists, overwrite? \r\n File : " + newPrefabMetaPath, "Overwrite"))
                {
                    Debug.LogWarning(
                        "Could not write the prefab as the file already exists.\r\n File: " + newPrefabMetaPath
                    );
                    return;
                }
            }
            File.Copy(currentPrefab.MetaPath, newPrefabMetaPath, true);

            string newPrefabPath = destination + Path.GetFileName(currentPrefab.Path);
            
            File.WriteAllText(newPrefabPath,
                string.Join("\r\n", parsedPrefab));
            Debug.Log("Written the prefab to : " + newPrefabPath);
        }
    }
}
#endif