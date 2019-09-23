using ExtensionMethods;
using YamlDotNet.Serialization;
#if UNITY_EDITOR
using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEditor;
using YamlDotNet.RepresentationModel;

namespace importerexporter
{
    /// <summary>
    /// Imports and exports the guids and fileIDS from projects
    /// </summary>
    public static class ImportExportUtility
    {
        /// <summary>
        /// Cached field of all assemblies to loop through
        /// </summary>
        private static Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();


        /// <summary>
        /// Gets all the classes in the project and gets the name of the class, the guid that unity assigned and the fileID.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static List<FileData> Export(string path)
        {
            float progress = 0;

            //Get all meta files
            var classMetaFiles = Directory.GetFiles(path, "*" +
                                                          ".cs.meta", SearchOption.AllDirectories);
            //Get all dlls
            var dllMetaFiles = Directory.GetFiles(path, "*" +
                                                        ".dll.meta", SearchOption.AllDirectories);

            int totalFiles = classMetaFiles.Length + dllMetaFiles.Length;

            List<FileData> data = new List<FileData>();
            foreach (string file in classMetaFiles)
            {
                progress++;
                EditorUtility.DisplayProgressBar("Exporting IDs", "Exporting IDs " + Path.GetFileName(file), progress / totalFiles);
                var lines = File.ReadAllLines(file);

                foreach (string line in lines)
                {
                    Regex regex = new Regex(@"(?<=guid: )[A-z0-9]*");
                    Match match = regex.Match(line);
                    if (match.Success)
                    {
                        string className = getClassByFile(file);
                        if (String.IsNullOrEmpty(className))
                        {
                            continue;
                        }

                        data.Add(new FileData(className, match.Value));
                    }
                }
            }

            // Loop through dlls

            foreach (string metaFile in dllMetaFiles)
            {
                progress++;
                EditorUtility.DisplayProgressBar("Exporting IDs", "Exporting IDs " + Path.GetFileName(metaFile), progress / totalFiles);
                string text = File.ReadAllText(metaFile);
                Regex regex = new Regex(@"(?<=guid: )[A-z0-9]*");
                Match match = regex.Match(text);
                if (!match.Success)
                {
                    throw new NotImplementedException("Could not parse the guid from the dll meta file. File : " +
                                                      metaFile);
                }

                var file = metaFile.Replace(".meta", "");
                try
                {
                    Assembly assembly = Assembly.LoadFile(file);
                    foreach (Type type in assembly.GetTypes())
                    {
                        EditorUtility.DisplayProgressBar("Exporting IDs", "Exporting IDs " + type, progress / totalFiles);
                        data.Add(new FileData(type.FullName, match.Value, FileIDUtil.Compute(type).ToString()));
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Could not load assembly : " + file + "\nException : " + e);
                }
            }

            EditorUtility.ClearProgressBar();
            return data;
        }


        /// <summary>
        /// Replaces all old GUIDs and old fileIDs with the new GUID and fileID and returns a the new scenefile.
        /// This can be saved as an .unity file and then be opened in the editor.
        /// </summary>
        /// <param name="fileToChange"></param>
        /// <param name="oldIDs"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static string[] Import(string fileToChange, List<FileData> oldIDs)
        {
            EditorUtility.DisplayProgressBar("Import progress bar", "Importing progress bar.", 0.5f);
            if (oldIDs == null)
            {
                throw new NotImplementedException("ExistingData is null");
            }

            var currentIDs = Export(Application.dataPath);
            var linesToChange = File.ReadAllLines(fileToChange);

            linesToChange = migrateGUIDsAndFieldIDs(linesToChange, currentIDs, oldIDs);
            linesToChange = migrateFieldData(linesToChange, currentIDs);


            EditorUtility.ClearProgressBar();

            return linesToChange;
        }

        public static string[] testVariableMapping(string path, List<FileData> currentIDS)
        {
            string[] lines = File.ReadAllLines(path);
            return migrateFieldData(lines, currentIDS);
        }

        private static string[] migrateGUIDsAndFieldIDs(string[] linesToChange, List<FileData> currentIDs,
            List<FileData> oldIDs)
        {
            for (var i = 0; i < linesToChange.Length; i++)
            {
                string line = linesToChange[i];
                Regex regexGuid = new Regex(@"(?<=guid: )[A-z0-9]*");
                Match matchGuid = regexGuid.Match(line);
                if (!matchGuid.Success) continue;

                Regex fileIDRegex = new Regex(@"(?<=fileID: )\-?[A-z0-9]*");

                var fileIDMatch = fileIDRegex.Match(line);
                string fileID = fileIDMatch.Success ? fileIDMatch.Value : "";


                FileData replacementFileData = getNewValue(oldIDs, currentIDs, fileID, matchGuid.Value);
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

            return linesToChange;
        }

        private static string[] migrateFieldData(string[] linesToChange, List<FileData> currentIDs)
        {
            string content = string.Join("\n", linesToChange);

            YamlStream yamlStream = new YamlStream();
            yamlStream.Load(new StringReader(content));

            for (var i = 0; i < yamlStream.Documents.Count; i++)
            {
                YamlDocument document = yamlStream.Documents[i];

                //get if its a monobehaviour
                string type = document.GetName();
                if (type != "MonoBehaviour")
                {
                    continue;
                }

                //get which type it is
                //    get guid and fileID
                var script = document.RootNode.GetChildren()["MonoBehaviour"];

                string fileID = (string) script["m_Script"]["fileID"];
                string guid = (string) script["m_Script"]["guid"];
                
                //    get corresponding fileData
                var currentFileData = currentIDs.First(data => data.FileID == fileID && data.Guid == guid);

                List<MemberData> unmapped = new List<MemberData>();
                
                // check if all fields are present
                foreach (MemberData member in currentFileData.FieldDatas)
                {
                    foreach (KeyValuePair<YamlNode,YamlNode> pair in script.GetChildren())
                    {
                        if ((string) pair.Key == member.Name)
                        {
                            //todo : this won't work
                            unmapped.Add(member);
                        }
                    }
                }
                
                //if not check for a mapping
                List<string> yamlMembers = script.GetChildren().Select(pair => pair.Key.ToString()).ToList();
                Dictionary<string, MemberData> mappings = new Dictionary<string, MemberData>(); 
                foreach (MemberData member in unmapped)
                {
                    var closest = yamlMembers.OrderBy(yamlMember => Levenshtein.Compute(member.Name, yamlMember)).First();
                    mappings.Add(closest,member);
                }
                
            }


            return linesToChange;
        }


        /// <summary>
        /// Get the Type of a class by the name of the class. 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static string getClassByFile(string path)
        {
            string fileName = Path.GetFileName(path);
            fileName = fileName.Replace(".cs.meta", "");
            Type[] types = assemblies.SelectMany(x => x.GetTypes())
                .Where(x => x.Name == fileName).ToArray();

            if (types.Length == 0)
            {
                Debug.LogWarning("Could not find type with name : " + fileName);
                return null;
            }

            if (types.Length > 1)
            {
                // Check if they're monoBehaviours and if they are return those.
                List<Type> monoBehaviours = new List<Type>();
                foreach (Type type in types)
                {
                    if (type.IsSubclassOf(typeof(MonoBehaviour)) &&
                        // Apparently we sometimes use the same dll in the same project causing the same classes(including same namespace), using the same name.
                        // As this causes the same fileID we just use the first one
                        monoBehaviours.FirstOrDefault(mono => mono.FullName == type.FullName) == null)
                    {
                        monoBehaviours.Add(type);
                    }
                }

                switch (monoBehaviours.Count)
                {
                    case 0:
                        Debug.Log("Could not find any scripts of type " + fileName);
                        return null;
                    case 1:
                        return monoBehaviours[0].FullName;
                    case 2:
                        return EditorUtility.DisplayDialog("Select", "Choose which class to export",
                            monoBehaviours[0].FullName, monoBehaviours[1].FullName)
                            ? monoBehaviours[0].FullName
                            : monoBehaviours[1].FullName;
                    case 3:
                        var complexResult = EditorUtility.DisplayDialogComplex("Select", "Choose which class to export",
                            monoBehaviours[0].FullName, monoBehaviours[1].FullName, monoBehaviours[2].FullName);
                        return monoBehaviours[complexResult].FullName;
                    default:
                        Debug.LogError("More than 3 MonoBehaviours found for " + fileName +
                                       " grabbing the last from : " +
                                       String.Concat(monoBehaviours.Select(type => type.FullName).ToArray()));
                        return monoBehaviours.Last().FullName;
                }
            }

            Type last = types.Last();
            return last.FullName;
        }

        private static FileData getNewValue(List<FileData> oldData, List<FileData> newData, string fileId,
            string oldGuid)
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

            Debug.Log("Could not find old guid that matches the class with guid: " + oldGuid + " fileID : " + fileId);
            return null;
        }
    }
}
#endif