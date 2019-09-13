using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

public partial class NewProjectImportWindow : EditorWindow
{
    private static List<FileData> filedata = new List<FileData>();

    [MenuItem("ImportExport/New project import window")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(NewProjectImportWindow));
    }

    private static string jsonTextArea = "";

    void OnGUI()
    {
        if (GUILayout.Button("Test export with dll"))
        {
            var exportedData = export();
            var content = JsonConvert.SerializeObject(exportedData,Formatting.Indented);
            jsonTextArea = content;
            Debug.Log(content);
        }

        if (GUILayout.Button("Import"))
        {
            string path = EditorUtility.OpenFilePanel("title", "", "*");
            if (path.Length != 0)
            {
                var content = JsonConvert.DeserializeObject<List<FileData>>(jsonTextArea);
                import(path, content);
                Debug.Log("Imported data");
            }
            else
            {
                throw new NotImplementedException("Could not get file");
            }
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        jsonTextArea = EditorGUILayout.TextArea(jsonTextArea);
        EditorGUILayout.EndScrollView();
    }

    private Vector2 scrollPosition;
    private List<FileData> export()
    {
        var path = Application.dataPath;

        //Get all files
        var classMetaFiles = Directory.GetFiles(path, "*" +
                                                      ".cs.meta", SearchOption.AllDirectories);
        List<FileData> data = new List<FileData>();
        foreach (string file in classMetaFiles)
        {
            var lines = File.ReadAllLines(file);

            foreach (string line in lines)
            {
                Regex regex = new Regex(@"(?<=guid: )[A-z0-9]*");
                Match match = regex.Match(line);
                if (match.Success)
                {
                    string className = getClassByFile(file);
                    if (className == null)
                    {
                        continue;
                    }
                    data.Add(new FileData(className, match.Value));
                    Debug.Log("File: " + file + "; GUID: " + match.Value);
                }
            }
        }

        //Get all dlls
        var dllMetaFiles = Directory.GetFiles(path, "*" +
                                                    ".dll.meta", SearchOption.AllDirectories);

        foreach (string metaFile in dllMetaFiles)
        {
            string text = File.ReadAllText(metaFile);
            Regex regex = new Regex(@"(?<=guid: )[A-z0-9]*");
            Match match = regex.Match(text);
            if (!match.Success)
            {
                throw new NotImplementedException("Could not parse the guid from the dll meta file. File : " + metaFile);
            }

            var file = metaFile.Replace(".meta", "");
            Assembly assembly = Assembly.LoadFile(file);
//            Assembly assembly = Assembly.ReflectionOnlyLoadFrom(file);
            foreach (Type type in assembly.GetTypes())
            {
                data.Add(new FileData(type.FullName, match.Value, FileIDUtil.Compute(type).ToString()));
            }
        }


        return data;
    }

    private string getClassByFile(string path)
    {
        var fileName = Path.GetFileName(path);
        fileName = fileName.Replace(".cs.meta", "");
        Type[] types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
            .Where(x => x.Name == fileName).ToArray();

        if (types.Length == 0)
        {
            Debug.Log("Could not find type with name : " + fileName);
            return null;
        }        
        if (types.Length > 1)
        {
            Debug.Log("Cannot find class with the name : " + fileName +
                                " as it has a double declaration in multiple namespaces. Using the last one");
        }
        var last = types.Last();
        return last.FullName;
    }

    private void import(string fileToChange, List<FileData> existingData)
    {
        if (existingData == null)
        {
            throw new NotImplementedException("ExistingData is null");
        }

        var linesToChange = File.ReadAllLines(fileToChange);
        var currentFileData = export();
        for (var i = 0; i < linesToChange.Length; i++)
        {
            string line = linesToChange[i];
            Regex regexGuid = new Regex(@"(?<=guid: )[A-z0-9]*");
            Match matchGuid = regexGuid.Match(line);
            if (!matchGuid.Success) continue;

            Regex fileIDRegex = new Regex(@"(?<=fileID: )\-?[A-z0-9]*");

            var fileIDMatch = fileIDRegex.Match(line);
            string fileID = fileIDMatch.Success ? fileIDMatch.Value : "";


            FileData replacementFileData = getNewValue(existingData, currentFileData, fileID, matchGuid.Value);
            if (replacementFileData == null)
            {
                continue;
            }

            // Replace the Guid
            linesToChange[i] = linesToChange[i].Replace(matchGuid.Value, replacementFileData.Guid);

            if (String.IsNullOrEmpty(fileID)) continue;

            //Replace the fileID
            linesToChange[i] = linesToChange[i].Replace(fileID, replacementFileData.FileID);
        }

        var now = DateTime.Now;
        File.WriteAllLines(fileToChange +
                           now.Hour + "_" + now.Minute + "_" + now.Minute + "_" + now.Second + ".unity",
            linesToChange);
    }


    private FileData getNewValue(List<FileData> oldData, List<FileData> newData, string fileId, string oldGuid)
    {
        FileData oldFileData = null;
        foreach (FileData currentOldFileData in oldData)
        {
            if ((currentOldFileData.Guid.Equals(oldGuid) && string.IsNullOrEmpty(fileId))
                || (currentOldFileData.Guid.Equals(oldGuid) && currentOldFileData.FileID.Equals(fileId)))
            {
                oldFileData = currentOldFileData;
                break;
            }
        }

        if (oldFileData != null)
        {
            var newFileData = newData.First(filedata => filedata.Name.Equals(oldFileData.Name));
            return newFileData;
        }

        Debug.Log("Could not find oldFileData");
        return null;
    }
}