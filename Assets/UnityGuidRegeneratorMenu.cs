 /*
     
    BACK UP YOUR PROJECT
     
    */


using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class UnityGuidRegeneratorMenu : MonoBehaviour
{
    [MenuItem("Tools/Regenerate asset GUIDs")]
    public static void RegenerateGuids()
    {
        if (EditorUtility.DisplayDialog("GUIDs regeneration",
 
            "You are going to start the process of GUID regeneration. This may have unexpected results. \n\n MAKE A PROJECT BACKUP BEFORE PROCEEDING!",
            "Regenerate GUIDs", "Cancel"))
        {
            try
            {
                AssetDatabase.StartAssetEditing();
                string path = Path.GetFullPath(".") + Path.DirectorySeparatorChar + "Assets";
                RegenerateGuids(path);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }
        }
    }

    private static readonly string[] fileListPath = {"*.meta", "*.mat", "*.anim", "*.prefab", "*.unity", "*.asset"};

    static string[] oldGUIDsList = new string[1] {"74dfce233ddb29b4294c3e23c1d3650d"};
    static string[] newGUIDsList = new string[1] {"89f0137620f6af44b9ba852b4190e64e"};

    static string[] oldFileIDsList = new string[1] {"11500000"};
    static string[] newFileIDsList = new string[1] {"-667331979"};

    static string _assetsPath;
    static Dictionary<string, string> GUIDDict = new Dictionary<string, string>();
    static Dictionary<string, string> FileIDDict = new Dictionary<string, string>();

    public static void RegenerateGuids(string path)
    {
        //Debug.Log ("Init.");
        for (int i = 0; i < oldGUIDsList.Length; i++)
            GUIDDict.Add(oldGUIDsList[i], newGUIDsList[i]);

        for (int i = 0; i < oldFileIDsList.Length; i++)
            FileIDDict.Add(oldFileIDsList[i], newFileIDsList[i]);

        //Get the list of files to modify
        _assetsPath = path;
        Debug.Log("Read File List: " + _assetsPath);
        //string[] fileList = File.ReadAllLines(_assetsPath + fileListPath);
        // Get list of working files
        List<string> fileList = new List<string>();
        foreach (string extension in fileListPath)
        {
            fileList.AddRange(Directory.GetFiles(_assetsPath, extension, SearchOption.AllDirectories));
        }

        //Debug.Log ("GUI Start for each");
        foreach (string f in fileList)
        {
            //Debug.Log ("file: " + f);
            string[] fileLines = File.ReadAllLines(f);

            for (int i = 0; i < fileLines.Length; i++)
            {
                bool GUIReplaced = false;
                //find all instances of the string "guid: " and grab the next 32 characters as the old GUID
                if (fileLines[i].Contains("guid: "))
                {
                    int index = fileLines[i].IndexOf("guid: ") + 6;
                    string oldGUID = fileLines[i].Substring(index, 32); // GUID has 32 characters.
                    //use that as a key to the dictionary and find the value
                    //replace those 32 characters with the new GUID value
                    if (GUIDDict.ContainsKey(oldGUID))
                    {
                        fileLines[i] = fileLines[i].Replace(oldGUID, GUIDDict[oldGUID]);
                        GUIReplaced = true;
                        Debug.Log("replaced GUID \"" + oldGUID + "\" with \"" + GUIDDict[oldGUID] + "\" in file " + f);
                    }

                    //else Debug.Log("GUIDDict did not contain the key " + oldGUID);
                }

                if (GUIReplaced && fileLines[i].Contains("fileID: "))
                {
                    int index = fileLines[i].IndexOf("fileID: ") + 8;
                    int index2 = fileLines[i].IndexOf(",", index);
                    string oldFileID = fileLines[i].Substring(index, index2 - index); // GUID has 32 characters.
                    //Debug.Log("FileID: "+oldFileID);
                    //use that as a key to the dictionary and find the value
                    //replace those 32 characters with the new GUID value
                    if (FileIDDict.ContainsKey(oldFileID))
                    {
                        fileLines[i] = fileLines[i].Replace(oldFileID, FileIDDict[oldFileID]);
                        Debug.Log("replaced FileID \"" + oldFileID + "\" with \"" + FileIDDict[oldFileID] +
                                  "\" in file " + f);
                    }

                    //else Debug.Log("FileIDDict did not contain the key " + oldFileID);
                }
            }

            //Write the lines back to the file
            File.WriteAllLines(f, fileLines);
        }
    }
}