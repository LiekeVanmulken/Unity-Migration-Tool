﻿using System.Collections.Generic;
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
        string oldIDs = EditorUtility.OpenFilePanel("Old IDs", Application.dataPath, "json");
        if (string.IsNullOrEmpty(oldIDs))
        {
            Debug.Log("No old ID path selected. Aborting the mapping.");
            return;
        }

        string newIDs = EditorUtility.OpenFilePanel("New IDs", Application.dataPath, "json");
        if (string.IsNullOrEmpty(newIDs))
        {
            Debug.Log("No new ID path selected. Aborting the mapping.");
            return;
        }
        MapAllClasses(oldIDs, newIDs);
    }

    /// <summary>
    /// Generate a mapping of all foundScripts in a project.
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
    /// Generate a mapping of all foundScripts in a project.
    /// Which means it creates a mapping between versions.
    /// </summary>
    /// <param name="oldIDs"></param>
    /// <param name="newIDs"></param>
    /// <returns></returns>
    public void MapAllClasses(List<ClassModel> oldIDs, List<ClassModel> newIDs)
    {
        string rootPath = Application.dataPath;
        ThreadUtil.RunThread(() =>
        {
            MigrationWindow.DisplayProgressBar("starting migration export", "Mapping classes", 0.4f);
            mappingController.MapAllClasses(oldIDs, newIDs, 
                mergedFoundScripts =>
            {
                SaveFoundScripts(rootPath, mergedFoundScripts);
                MigrationWindow.ClearProgressBar();
                ThreadUtil.RunMainThread(() =>
                    {
                        EditorUtility.DisplayDialog("Completed mapping",
                            "Completed the mapping. Saved the mapping to :" + constants.RelativeFoundScriptPath, "Ok");
                    });
            });
        });
    }


    /// <summary>
    /// Write foundScripts to a file
    /// </summary>
    /// <param name="rootPath"></param>
    /// <param name="foundScripts"></param>
    public void SaveFoundScripts(string rootPath, List<FoundScript> foundScripts)
    {
        string foundScriptsPath = rootPath + constants.RelativeFoundScriptPath;
        File.WriteAllText(foundScriptsPath, JsonConvert.SerializeObject(foundScripts, constants.IndentJson));
    }
}