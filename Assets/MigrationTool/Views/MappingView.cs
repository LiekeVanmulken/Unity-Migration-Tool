using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using migrationtool.controllers;
using migrationtool.models;
using migrationtool.utility;
using migrationtool.windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        string rootPath = Application.dataPath;
        ThreadUtil.RunThread(() =>
        {
            MigrationWindow.DisplayProgressBar("starting migration export", "Mapping classes", 0.4f);
            mappingController.MapAllClasses(oldIDs, newIDs,
                mergedScriptMapping =>
                {
                    SaveScriptMappings(rootPath, mergedScriptMapping);
                    MigrationWindow.ClearProgressBar();
                    ThreadUtil.RunMainThread(() =>
                    {
                        EditorUtility.DisplayDialog("Completed mapping",
                            "Completed the mapping. Saved the mapping to :" + constants.RelativeScriptMappingPath,
                            "Ok");
                    });
                });
        });
    }

    public List<ScriptMapping> GenerateNewMapping(string oldVersion, string newVersion)
    {
        Dictionary<string, List<ScriptMapping>> allVersions = new Dictionary<string, List<ScriptMapping>>();
        WebClient wc = new WebClient();
        using (MemoryStream stream = new MemoryStream(wc.DownloadData("http://localhost:8080/versions")))
        {
            string request = Encoding.ASCII.GetString(stream.ToArray());
            Debug.LogError(request);
            JArray data = JArray.Parse(request);
            foreach (JObject element in data)
            {
                string version = (string) element["version"];
                string mappingUrl = (string) element["mappingUrl"];
                using (MemoryStream linkStream = new MemoryStream(wc.DownloadData(mappingUrl)))
                {
                    Debug.Log("Downloaded mapping Version: " + version + " Url: " + mappingUrl);
                    string mappingString = Encoding.ASCII.GetString(linkStream.ToArray());
                    
                    List<ScriptMapping> mapping =  JsonConvert.DeserializeObject<List<ScriptMapping>>(mappingString);
                    allVersions[version] = mapping;
                }
            }
        }

        return mappingController.CombineMappings(allVersions);
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
}