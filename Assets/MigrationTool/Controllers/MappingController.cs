
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using migrationtool.models;
using migrationtool.utility;
using migrationtool.windows;
using Newtonsoft.Json;
using UnityEngine;


namespace migrationtool.controllers
{
    public class MappingController
    {
        IDController idController = new IDController();

        /// <summary>
        /// Generate a mapping of all scriptMappings in a project.
        /// Which means it creates a mapping between versions.
        /// </summary>
        /// <param name="oldIDs"></param>
        /// <param name="newIDs"></param>
        /// <returns></returns>
        public void MapAllClasses(List<ClassModel> oldIDs, List<ClassModel> newIDs,
            Action<List<ScriptMapping>> onComplete)
        {
            if (oldIDs == null || newIDs == null)
            {
                Debug.LogError("Old or new IDS are null. Cannot map without the old and new IDs");
            }

            List<ScriptMapping> unmergedScriptMappings = MappAllClasses(oldIDs, newIDs);
            List<ScriptMapping> unmappedScriptMapping = unmergedScriptMappings
                .Where(script => script.HasBeenMapped == ScriptMapping.MappedState.NotMapped).ToList();
            if (unmappedScriptMapping.Count == 0)
            {
                onComplete(unmappedScriptMapping);
                return;
            }

            ThreadUtil.RunMainThread(() =>
            {
                MergeWizard mergeWizard = MergeWizard.CreateWizard(unmappedScriptMapping);
                mergeWizard.onComplete = (mergedScriptMapping) =>
                {
                    List<ScriptMapping> completed = unmappedScriptMapping.Merge(mergedScriptMapping);
                    onComplete(completed);
                };
            });
        }

        /// <summary>
        /// Maps all classes to scriptMappings in a project
        /// </summary>
        /// <param name="oldIDs"></param>
        /// <param name="newIDs"></param>
        /// <returns></returns>
        private List<ScriptMapping> MappAllClasses(List<ClassModel> oldIDs, List<ClassModel> newIDs)
        {
            List<ScriptMapping> scriptMappings = new List<ScriptMapping>();
            List<ClassModel> classesToMap = new List<ClassModel>();
            foreach (ClassModel oldID in oldIDs)
            {
                ClassModel result = idController.FindNewID(newIDs, oldID);
                if (result == null)
                {
                    classesToMap.Add(oldID);
                }

                idController.FindMappingRecursively(newIDs, ref scriptMappings, oldID);
            }

            GC.Collect();

            if (classesToMap.Count > 0)
            {
                Debug.Log(string.Join("\r\n", classesToMap.Select(model => model.FullName).ToArray()));
            }

            Debug.Log("Mapped classes. Total: " + oldIDs.Count + " classesThatNeedToBeMapped: " + classesToMap.Count);


            return scriptMappings;
        }

        public static List<ScriptMapping> DeserializeMapping(string path)
        {
            return JsonConvert.DeserializeObject<List<ScriptMapping>>(File.ReadAllText(path));
        }

        /// <summary>
        /// Checks if the version is between the old and new version or is the old or new version 
        /// </summary>
        /// <param name="version"></param>
        /// <param name="oldVersion"></param>
        /// <param name="newVersion"></param>
        /// <returns></returns>
        public bool IsInsideVersions(string version, string oldVersion, string newVersion)
        {
            return (IsOriginalVersionHigher(version, oldVersion) == 1 || version == oldVersion)
                   &&
                   (IsOriginalVersionHigher(version, newVersion) == -1 || version == newVersion);
        }

        /// <summary>
        /// Combine multiple mappings to one
        /// </summary>
        /// <param name="allScriptMappings">A dictionary where the Key is the version and the Value is a list of the scriptMappings</param>
        /// <param name="oldVersion"></param>
        /// <param name="newVersion"></param>
        /// <returns></returns>
        public List<ScriptMapping> CombineMappings(Dictionary<string, List<ScriptMapping>> allScriptMappings,
            string oldVersion, string newVersion)

        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (allScriptMappings.Count)
            {
                case 0:
                    Debug.LogError("No mappings found, user will have to create the mappings when necessary..");
                    return new List<ScriptMapping>();
                case 1:
                    Debug.LogWarning("No Mappings combined, only a single mapping available.");
                    return allScriptMappings.First().Value;
            }

            List<string> keysToRemove = new List<string>();
            foreach (var version in allScriptMappings)
            {
                if (IsInsideVersions(version.Key, oldVersion, newVersion))
                {
                    continue;
                }

                keysToRemove.Add(version.Key);
            }

            foreach (string key in keysToRemove)
            {
                allScriptMappings.Remove(key);
            }

            KeyValuePair<string, List<ScriptMapping>>[] orderedList = allScriptMappings
                .ToList()
                .OrderBy(pair => pair.Key).ToArray();
            List<List<ScriptMapping>> sortedScriptMappings = orderedList.Select(pair => pair.Value).ToList();

            var combinedScriptMapping = sortedScriptMappings[0];


            for (var i = 1; i < sortedScriptMappings.Count; i++)
            {
                List<ScriptMapping> updatedMapping = sortedScriptMappings[i];
                List<ScriptMapping> scriptMappingsToAdd = new List<ScriptMapping>();
                foreach (ScriptMapping newMapping in updatedMapping)
                {
                    ScriptMapping oldMapping = combinedScriptMapping.FirstOrDefault(
                        old => old.newClassModel.FullName == newMapping.oldClassModel.FullName);
                    if (oldMapping == null)
                    {
                        scriptMappingsToAdd.Add(newMapping);
                        continue;
                    }

                    oldMapping.MergeNodes = CombineMergeNodes(oldMapping.MergeNodes, newMapping.MergeNodes);
                    oldMapping.newClassModel = newMapping.newClassModel;
                }

                combinedScriptMapping.AddRange(scriptMappingsToAdd);
            }

            return combinedScriptMapping;
        }

        private List<MergeNode> CombineMergeNodes(List<MergeNode> oldMergeNodes, List<MergeNode> newMergeNodes)
        {
            List<MergeNode> mergeNodesToAdd = new List<MergeNode>();
            foreach (MergeNode newMergeNode in newMergeNodes)
            {
                MergeNode mergeNode =
                    oldMergeNodes.FirstOrDefault(old => old.NameToExportTo == newMergeNode.OriginalValue);
                if (mergeNode == null)
                {
                    mergeNodesToAdd.Add(newMergeNode);
                    Debug.Log("Could not find mergeNode for new mergeNode: " + newMergeNode.OriginalValue +
                              " adding new mergeNodes.");
                    continue;
                }

                mergeNode.NameToExportTo = newMergeNode.NameToExportTo;
            }

            oldMergeNodes.AddRange(mergeNodesToAdd);
            return oldMergeNodes;
        }

        public bool IsOldVersionHigher(string oldVersion, string newVersion)
        {
            return IsOriginalVersionHigher(oldVersion, newVersion) == 1;
        }

        /// <summary>
        /// Checks whether the original has a higher version then the changed
        /// </summary>
        /// <param name="original"></param>
        /// <param name="changed"></param>
        /// <returns>
        ///  1: if original is larger
        ///  0: if they're the same
        /// -1: if changed is larger
        /// </returns>
        /// <exception cref="FormatException"></exception>
        private int IsOriginalVersionHigher(string original, string changed)
        {
            string[] originalSplit = original.Split('.');
            string[] changedSplit = changed.Split('.');

            if (originalSplit.Length != 4 || changedSplit.Length != 4)
            {
                throw new FormatException("original or changed version are not the correct format");
            }

            for (int i = 0; i < 4; i++)
            {
                int originalCurrent = Int32.Parse(originalSplit[i]);
                int changedCurrent = Int32.Parse(changedSplit[i]);
                if (originalCurrent > changedCurrent)
                {
                    return 1;
                }

                if (originalCurrent < changedCurrent)
                {
                    return -1;
                }
            }

            return 0;
        }
    }
}