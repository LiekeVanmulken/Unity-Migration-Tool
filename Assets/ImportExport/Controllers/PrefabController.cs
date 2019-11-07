using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using importerexporter.models;
using importerexporter.utility;
using importerexporter.windows;
using UnityEditor;
using UnityEngine;

public class PrefabController : Singleton<PrefabController>
{
    private readonly Constants constants = Constants.Instance;

    public bool CopyAllPrefabs(string originalPath, string destinationPath)
    {
        try
        {
            string[] prefabMetaFiles = Directory.GetFiles(originalPath, "*.prefab.meta", SearchOption.AllDirectories);
            foreach (string file in prefabMetaFiles)
            {
                CopyPrefab(file, destinationPath);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Could not copy the prefab files, Error : \r\n" + e);
            return false;
        }

        return true;
    }

    public void CopyPrefab(string originalPath, string destinationPath)
    {
        if (!originalPath.EndsWith(".prefab.meta"))
        {
            Debug.LogError("Can not move file that does not have a meta file. Given path : " + originalPath);
            return;
        }

        CopyFile(originalPath, destinationPath);
        CopyFile(originalPath.Replace(".meta", ""), destinationPath);
    }

    private static void CopyFile( string originalFile,string destinationPath)
    {
        string fileDestination = Path.Combine(destinationPath, Path.GetFileName(originalFile));
        if (File.Exists(fileDestination))
        {
            if (EditorUtility.DisplayDialog("Prefab already exists",
                "Prefab file already exists, Do you want to overwrite the file : \r\n" + fileDestination +
                " \r\r Original location : " + originalFile, "Overwrite", "Ignore"))
            {
                File.Copy(originalFile, fileDestination, true);
            }
            else
            {
                Debug.Log("Skipped prefab : " + originalFile);
            }
        }
        else
        {
            File.Copy(originalFile, fileDestination);
        }
    }

    public List<PrefabModel> ExportPrefabs(string path)
    {
        //Get all prefabs
        string[] prefabMetaFiles = Directory.GetFiles(path, "*.prefab.meta", SearchOption.AllDirectories);

        List<PrefabModel> prefabModels = new List<PrefabModel>(prefabMetaFiles.Length);
        for (var i = 0; i < prefabMetaFiles.Length; i++)
        {
            string file = prefabMetaFiles[i];
//            MigrationWindow.DisplayProgressBar("Exporting Prefabs", "Exporting prefab " + Path.GetFileName(file),
//                prefabMetaFiles.Length / i);

            ParsePrefabFile(file, ref prefabModels);
        }

        return prefabModels;
    }

    private void ParsePrefabFile(string file, ref List<PrefabModel> data)
    {
        IEnumerable<string> lines = File.ReadLines(file);
        foreach (string line in lines)
        {
            Match match = constants.RegexGuid.Match(line);
            if (!match.Success) continue;

            data.Add(new PrefabModel(file, match.Value));
            break;
        }
    }
}