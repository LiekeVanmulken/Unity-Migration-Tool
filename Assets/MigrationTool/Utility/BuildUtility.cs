using System;
using System.Collections.Generic;
using System.IO;
using migrationtool.controllers;
using migrationtool.models;
using migrationtool.views;
using Newtonsoft.Json;
using UnityEngine;

namespace migrationtool.utility
{
    /// <summary>
    /// Exposes certain translations to migrate inside PREspective
    /// todo : find a way to keep this outside of the migration tool as this is really PREspective specific 
    /// </summary>
    public class BuildUtility
    {
        /// <summary>
        /// Exports the classes of the project
        /// </summary>
        /// <param name="projectPath"></param>
        /// <returns></returns>
        public static List<ClassModel> ExportClassData(string projectPath)
        {
            return new IDController().ExportClassData(projectPath);
        }

        /// <summary>
        /// Exposes the export from outside to call it from a build.
        /// And saves it to the <see cref="exportPath"/>
        /// </summary>
        /// <param name="exportPath"></param>
        public static void ExportClassData(string projectPath, string exportPath)
        {
            List<ClassModel> export = ExportClassData(projectPath);
            string jsonExport = JsonConvert.SerializeObject(export, Formatting.Indented);

            if (!Directory.Exists(Path.GetDirectoryName(exportPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(exportPath));
            }

            File.WriteAllText(exportPath, jsonExport);

            Debug.Log("Exported the classes of path : " + projectPath + " \r\nTo export: " + exportPath);
        }

        /// <summary>
        /// Transforms the prefabs.
        /// Only changes the guid's and fileIDs as the fields and classes should be exactly the same
        /// </summary>
        /// <param name="originalExportFile"></param>
        /// <param name="destinationExportFile"></param>
        /// <param name="originalProjectPath"></param>
        /// <param name="destinationProjectPath"></param>
        /// <exception cref="FormatException"></exception>
        /// <exception cref="DirectoryNotFoundException"></exception>
        public static void TransformIDsOfPrefabs(string originalExportFile, string destinationExportFile,
            string originalProjectPath, string destinationProjectPath)
        {
            if (
                !originalExportFile.EndsWith(".json") ||
                !File.Exists(originalExportFile) ||
                !destinationExportFile.EndsWith(".json") ||
                !File.Exists(destinationExportFile)
            )
            {
                throw new FormatException("Could not read exports files. Please check the Exports.json");
            }

            if (!Directory.Exists(originalProjectPath) || !Directory.Exists(destinationProjectPath))
            {
                throw new DirectoryNotFoundException("Could not find the original or destination path");
            }

            List<ClassModel> oldIDs =
                JsonConvert.DeserializeObject<List<ClassModel>>(File.ReadAllText(originalProjectPath));
            List<ClassModel> newIDs =
                JsonConvert.DeserializeObject<List<ClassModel>>(File.ReadAllText(destinationExportFile));

            TransformIDsOfPrefabs(oldIDs, newIDs, originalProjectPath, destinationProjectPath);
        }

        /// <summary>
        /// Transforms the prefabs.
        /// Only changes the guid's and fileIDs as the fields and classes should be exactly the same
        /// </summary>
        /// <param name="oldIDs"></param>
        /// <param name="newIDs"></param>
        /// <param name="originalProjectPath"></param>
        /// <param name="destinationProjectPath"></param>
        public static void TransformIDsOfPrefabs(List<ClassModel> oldIDs, List<ClassModel> newIDs,
            string originalProjectPath, string destinationProjectPath)
        {
            List<PrefabModel> exportPrefabs = new PrefabController().ExportPrefabs(originalProjectPath);
            List<FoundScript> foundScripts = new List<FoundScript>();

            IDController idController = new IDController();
            PrefabView prefabView = new PrefabView();

            foreach (PrefabModel prefab in exportPrefabs)
            {
                string[] parsedPrefab = idController.TransformIDs(prefab.Path, oldIDs, newIDs, ref foundScripts);
                prefabView.WritePrefab(parsedPrefab, prefab, destinationProjectPath);
            }

            Debug.Log("Converted all prefabs to : " + destinationProjectPath);
        }
    }
}