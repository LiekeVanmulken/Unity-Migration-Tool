#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using migrationtool.controllers;
using migrationtool.models;
using migrationtool.utility;
using migrationtool.windows;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public class MappingView
{
    private readonly Constants constants = Constants.Instance;

    private readonly MappingController mappingController = new MappingController();

    public void MapAllClasses()
    {
        string oldIDs = EditorUtility.OpenFilePanel("Old IDs", constants.RootDirectory, "json");
        if (string.IsNullOrEmpty(oldIDs))
        {
            Debug.Log("No old ID path selected. Aborting the mapping.");
            return;
        }

        string newIDs = EditorUtility.OpenFilePanel("New IDs", constants.RootDirectory, "json");
        if (string.IsNullOrEmpty(newIDs))
        {
            Debug.Log("No new ID path selected. Aborting the mapping.");
            return;
        }

        MapAllClasses(oldIDs, newIDs);
    }

    /// <summary>
    /// Generate a mapping of all scriptMappings in a project.
    /// Which means it creates a mapping between versions.
    /// </summary>
    /// <param name="oldIDsPath"></param>
    /// <param name="newIDsPath"></param>
    /// <returns></returns>
    public void MapAllClasses(string oldIDsPath, string newIDsPath)
    {
        List<ClassModel> oldIDs = IDController.DeserializeIDs(oldIDsPath);
        List<ClassModel> newIDs = IDController.DeserializeIDs(newIDsPath);
        MapAllClasses(oldIDs, newIDs);
    }

    /// <summary>
    /// Generate a mapping of all scriptMappings in a project.
    /// Which means it creates a mapping between versions.
    /// </summary>
    /// <param name="oldIDs"></param>
    /// <param name="newIDs"></param>
    /// <returns></returns>
    public void MapAllClasses(List<ClassModel> oldIDs, List<ClassModel> newIDs)
    {
        ThreadUtil.RunThread(() =>
        {
            MigrationWindow.DisplayProgressBar("starting migration export", "Mapping classes", 0.4f);
            mappingController.MapAllClasses(oldIDs, newIDs,
                mergedScriptMapping =>
                {
                    SaveScriptMappings(constants.RootDirectory, mergedScriptMapping);
                    MigrationWindow.ClearProgressBar();
                    ThreadUtil.RunMainThread(() =>
                    {
                        EditorUtility.DisplayDialog("Completed mapping",
                            "Completed the mapping. Saved the mapping to: " + constants.RelativeScriptMappingPath,
                            "Ok");
                    });
                });
        });
    }

    public List<ScriptMapping> CombineMappings(string oldVersion, string newVersion,Dictionary<string, List<ScriptMapping>> allVersions)
    {
        List<ScriptMapping> newMapping = mappingController.CombineMappings(allVersions, oldVersion, newVersion);
        Debug.Log("Combined mappings of version " + oldVersion + " to " + newVersion);
        return newMapping;
    }


    /// <summary>
    /// Write scriptMappings to a file
    /// </summary>
    /// <param name="rootPath"></param>
    /// <param name="scriptMappings"></param>
    public void SaveScriptMappings(string rootPath, List<ScriptMapping> scriptMappings)
    {
        string scriptMappingsPath = rootPath + constants.RelativeScriptMappingPath;
        File.WriteAllText(scriptMappingsPath, JsonConvert.SerializeObject(scriptMappings, constants.IndentJson));
    }

    public bool IsOldVersionHigher(string oldVersion, string newVersion)
    {
        return mappingController.IsOldVersionHigher(oldVersion, newVersion);
    }
}
#endif