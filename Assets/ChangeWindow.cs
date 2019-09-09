using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

public class ChangeWindow : EditorWindow
{
    [Serializable]
    public class FileData
    {
        [SerializeField] private string path;


        public string Path
        {
            get { return path; }
            set
            {
                path = value;
                Name = System.IO.Path.GetFileName(path);
            }
        }

        [SerializeField] public string Name { get; set; }
        [SerializeField] public string FileID { get; set; }


        [SerializeField] public string Guid { get; set; }

        public FileData()
        {
        }

        public FileData(string path, string guid, bool generateFromPath = false)
        {
            path = path.Replace(".cs.meta", "");
            if (generateFromPath)
            {
                Path = path;
            }
            else
            {
                this.path = path;
                this.Name = path;
            }

            this.Guid = guid;
        }

        public FileData(string path, string guid, string fileID, bool generateFromPath = false)
        {
            path = path.Replace(".cs.meta", "");
            if (generateFromPath)
            {
                Path = path;
            }
            else
            {
                this.path = path;
                this.Name = path;
            }

            this.Guid = guid;
            this.FileID = fileID;
        }
    }

    private static List<FileData> filedata = new List<FileData>();

    [MenuItem("ImportExport/Window")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(ChangeWindow));
    }

    private static string jsonTextArea = "";

    void OnGUI()
    {
        jsonTextArea = EditorGUILayout.TextArea(jsonTextArea);

        if (GUILayout.Button("Export"))
        {
            filedata = export();
            var data = JsonConvert.SerializeObject(filedata, Formatting.Indented);
            EditorGUIUtility.systemCopyBuffer = data;
            jsonTextArea = data;
            Debug.Log("Exported content: \n" + data);
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
    }

    private List<FileData> export()
    {
        var path = Application.dataPath;
        var directories = Directory.GetFiles(path, "*.cs.meta", SearchOption.AllDirectories);
        List<FileData> data = new List<FileData>();

        foreach (string file in directories)
        {
            var lines = File.ReadAllLines(file);

            foreach (string line in lines)
            {
                Regex regex = new Regex(@"(?<=guid: )[A-z0-9]*");
                Match match = regex.Match(line);
                if (match.Success)
                {
                    data.Add(new FileData(Path.GetFileName(file), match.Value));
                    Debug.Log("File: " + file + "; GUID: " + match.Value);
                }
            }
        }

        return data;
    }

    private void import(string fileToChange, List<FileData> existingData)
    {
        if (existingData == null)
        {
            throw new NotImplementedException("test");
        }

        var linesToChange = File.ReadAllLines(fileToChange);

        var currentFileData = export();

        for (var i = 0; i < linesToChange.Length; i++)
        {
            string line = linesToChange[i];
            Regex regexGuid = new Regex(@"(?<=guid: )[A-z0-9]*");


            Match matchGuid = regexGuid.Match(line);
            if (!matchGuid.Success) continue;

            Regex fileIDRegex = new Regex(@"(?<=fileID: )[A-z0-9]*");

            var fileIDMatch = fileIDRegex.Match(line);
            string fileID = fileIDMatch.Success ? fileIDMatch.Value : "";

            
            
            FileData replacementFileData = getNewValue(existingData, currentFileData, fileID, matchGuid.Value);

            if (replacementFileData == null)
            {
                continue;
            }

            if (!String.IsNullOrEmpty(fileID) && replacementFileData.FileID!=null)
            {
                linesToChange[i] = line.Replace(fileID, replacementFileData.FileID);
            }

            linesToChange[i] = line.Replace(matchGuid.Value, replacementFileData.Guid);

        }

        var now = DateTime.Now;
        File.WriteAllLines(fileToChange +
                           now.Hour + "_" + now.Minute + "_" + now.Minute + "_" + now.Second + ".unity",
            linesToChange);
    }


    private FileData getNewValue(List<FileData> oldData, List<FileData> newData, string fileId, string oldGuid)
    {
        if (oldGuid.Equals("cc55f0e6497cb994f8838590aeb61b0f"))
        {
            Debug.Log("Should replace");
        }

        FileData oldFileData = null;
        foreach (FileData currentOldFileData in oldData)
        {
            if ((currentOldFileData.Guid.Equals(oldGuid) && string.IsNullOrEmpty(currentOldFileData.FileID))
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