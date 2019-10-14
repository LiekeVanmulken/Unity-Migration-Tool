
using static importerexporter.models.FoundScript;
#if UNITY_EDITOR
using YamlDotNet.RepresentationModel;
using importerexporter.models;
using importerexporter.utility;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using importerexporter.windows;

namespace importerexporter
{
    /// <summary>
    /// Imports and exports the guids and fileIDS from projects
    /// </summary>
    public class IDUtility
    {
        #region Singleton

        private static IDUtility instance = null;

        private static readonly object padlock = new object();

        IDUtility()
        {
        }

        public static IDUtility Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new IDUtility();
                    }

                    return instance;
                }
            }
        }

        #endregion

        private Constants constants = Constants.Instance;

        /// <summary>
        /// Cached field of all assemblies to loop through
        /// </summary>
        private Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        private readonly Regex regexGuid = new Regex(@"(?<=guid: )[A-z0-9]*");
        private readonly Regex regexFileID = new Regex(@"(?<=fileID: )\-?[A-z0-9]*");

        /// <summary>
        /// Gets all the classes in the project and gets the name of the class, the guid that unity assigned and the fileID.
        /// Then checks all to be serialized fields and returns them in a list
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public List<ClassData> ExportClassData(string path)
        {
            float progress = 0;

            //Get all meta files
            string[] classMetaFiles = Directory.GetFiles(path, "*" +
                                                               ".cs.meta", SearchOption.AllDirectories);
            //Get all dlls
            string[] dllMetaFiles = Directory.GetFiles(path, "*" +
                                                             ".dll.meta", SearchOption.AllDirectories);

            int totalFiles = classMetaFiles.Length + dllMetaFiles.Length;

            List<ClassData> data = new List<ClassData>();
            foreach (string file in classMetaFiles)
            {
                progress++;
                ImportWindow.DisplayProgressBar("Exporting IDs", "Exporting IDs " + Path.GetFileName(file),
                    progress / totalFiles);
                string[] lines = File.ReadAllLines(file);

                foreach (string line in lines)
                {
                    Match match = regexGuid.Match(line);
                    if (!match.Success) continue;

                    string className = getTypeByMetafileFileName(file);
                    if (String.IsNullOrEmpty(className))
                    {
                        continue;
                    }

                    data.Add(new ClassData(className, match.Value));
                }
            }

            //todo : uncomment, commented for speed with debugging
            // Loop through dlls  
            if (!constants.DEBUG)
            {
                foreach (string metaFile in dllMetaFiles)
                {
                    progress++;
                    ImportWindow.DisplayProgressBar("Exporting IDs", "Exporting IDs " + Path.GetFileName(metaFile),
                        progress / totalFiles);
                    string text = File.ReadAllText(metaFile);
                    Match match = regexGuid.Match(text);
                    if (!match.Success)
                    {
                        Debug.LogError("Could not parse the guid from the dll meta file. File : " +
                                       metaFile);
                    }

                    string file = metaFile.Replace(".meta", "");
                    try
                    {
                        Assembly assembly = Assembly.LoadFile(file);
                        foreach (Type type in assembly.GetTypes())
                        {
                            ImportWindow.DisplayProgressBar("Exporting IDs", "Exporting IDs " + type,
                                progress / totalFiles);
                            data.Add(new ClassData(type.FullName, match.Value, FileIDUtil.Compute(type).ToString()));
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("Could not load assembly : " + file + "\nException : " + e);
                    }
                }
            }

            ImportWindow.ClearProgressBar();
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
        public string[] ImportClassDataAndTransformIDs(string fileToChange, List<ClassData> oldIDs,
            List<ClassData> newIDs, ref List<FoundScript> foundScripts)
        {
            ImportWindow.DisplayProgressBar("Migration started",
                "Start importing current project classData and migrating scene.", 0.5f);
            if (oldIDs == null || newIDs == null || foundScripts == null)
            {
                throw new NotImplementedException("Some of the data with which to export is null.");
            }

            string[] linesToChange = File.ReadAllLines(fileToChange);

            linesToChange = MigrateGUIDsAndFieldIDs(linesToChange, oldIDs, newIDs, ref foundScripts);
            ImportWindow.ClearProgressBar();

            return linesToChange;
        }


        /// <summary>
        /// Replaces the GUID and fileID, matching the oldIDs with the currentIDs
        /// </summary>
        /// <param name="linesToChange"></param>
        /// <param name="oldIDs">List of GUIDs and FileID for all classes in the previous project.</param>
        /// <param name="newIDs">List of GUIDs and FileID for all currently in the project classes.</param>
        /// <param name="foundScripts"></param>
        /// <returns></returns>
        private string[] MigrateGUIDsAndFieldIDs(string[] linesToChange, List<ClassData> oldIDs, List<ClassData> newIDs,
            ref List<FoundScript> foundScripts)
        {
            string sceneContent = string.Join("\n", linesToChange);

            YamlStream yamlStream = new YamlStream();
            yamlStream.Load(new StringReader(sceneContent));
            List<YamlDocument> yamlDocuments =
                yamlStream.Documents.Where(document => document.GetName() == "MonoBehaviour").ToList();
            foreach (YamlDocument document in yamlDocuments)
            {
                YamlNode monoBehaviour = document.RootNode.GetChildren()["MonoBehaviour"]; //todo : duplicate code, fix 

                YamlNode oldFileIdNode = monoBehaviour["m_Script"]["fileID"];
                YamlNode oldGuidNode = monoBehaviour["m_Script"]["guid"];

                string oldFileId = oldFileIdNode.ToString();
                string oldGuid = oldGuidNode.ToString();

                FoundScript existingFoundScript = foundScripts.FirstOrDefault(script =>
                    script.OldClassData.Guid == oldGuid && script.OldClassData.FileID == oldFileId);

                ClassData replacementClassData =
                    existingFoundScript?.NewClassData ?? findNewID(oldIDs, newIDs, oldFileId, oldGuid);

                if (existingFoundScript == null)
                {
                    existingFoundScript = new FoundScript
                    {
                        OldClassData =
                            oldIDs.First(data =>
                                data.Guid == oldGuid &&
                                data.FileID ==
                                oldFileId), 
                        NewClassData = replacementClassData
                    };
                    MappedState hasBeenMapped = existingFoundScript.CheckHasBeenMapped();
                    if (hasBeenMapped == MappedState.NotMapped)
                    {
                        existingFoundScript.GenerateMappingNode();
                    }
                    foundScripts.Add(existingFoundScript);
                }

                int line = oldFileIdNode.Start.Line - 1;

                // Replace the Guid
                linesToChange[line] = linesToChange[line].ReplaceFirst(oldGuid, replacementClassData.Guid);

                if (String.IsNullOrEmpty(oldFileId)) continue;

                //Replace the fileID
                linesToChange[line] = linesToChange[line].ReplaceFirst(oldFileId, replacementClassData.FileID);
            }

            return linesToChange;
        }


        /// <summary>
        /// Get the Type of a class by the name of the class.
        /// </summary>
        /// <param name="path">The path of the meta file we're getting the name from</param>
        /// <returns></returns>
        private string getTypeByMetafileFileName(string path)
        {
            string fileName = Path.GetFileName(path);
            fileName = fileName.Replace(".cs.meta", "");
            string fileNameLower = fileName.ToLower();

            List<Type> types = assemblies.SelectMany(x => x.GetTypes())
                .Where(x => x.Name.ToLower() == fileNameLower).ToList();
            if (types.Count == 0)
            {
                Debug.Log("Checked for type  \"" + fileName +
                          "\" no types were found."); //todo : should this also give a popup?
                return null;
            }

            if (types.Count == 1)
            {
                return types[0].FullName;
            }

            // Check if they're monoBehaviours and if they are return those.
            List<Type> monoBehaviours = new List<Type>();
            foreach (Type type in types)
            {
                if (type.IsSubclassOf(typeof(MonoBehaviour)) &&
                    // Apparently we sometimes use the same dll in the same project causing the same classes(including same namespace), using the same name.
                    // As this causes the same fileID we just use the first one
                    monoBehaviours.FirstOrDefault(mono => mono.Name == type.Name) == null)
                {
                    monoBehaviours.Add(type);
                }
            }

            if (monoBehaviours.Count == 0)
            {
                Debug.LogWarning("Class : " + fileName +
                                 " could not be found and is not an MonoBehaviour so will skip");
                return null;
            }

            if (monoBehaviours.Count == 1)
            {
                return monoBehaviours[0].FullName;
            }

            string[] options = monoBehaviours.Select(type => type.FullName).ToArray();
            return ImportWindow.OpenOptionsWindow("Class cannot be found, select which one to choose", fileName,
                options);
        }

        /// <summary>
        /// Finds the new GUID and fileID from the old IDs and the new IDs by checking for the classname in both
        /// </summary>
        /// <param name="oldIDs"></param>
        /// <param name="newIDs"></param>
        /// <param name="oldFileID"></param>
        /// <param name="oldGuid"></param>
        /// <returns></returns>
        public ClassData findNewID(List<ClassData> oldIDs, List<ClassData> newIDs, string oldFileID,
            string oldGuid)
        {
            ClassData oldClassData = oldIDs.FirstOrDefault(
                currentOldFileData => currentOldFileData.Guid.Equals(oldGuid) &&
                                      oldFileID.Equals("11500000")
                                      ||
                                      currentOldFileData.Guid.Equals(oldGuid) &&
                                      currentOldFileData.FileID.Equals(oldFileID)
            );
            if (oldClassData != null)
            {
                ClassData newFileData = newIDs.FirstOrDefault(filedata => filedata.Name.Equals(oldClassData.Name));
                if (newFileData != null) return newFileData;

                ClassData[] closest = newIDs
                    .OrderByDescending(data => Levenshtein.Compute(data.Name, oldClassData.Name))
                    .ToArray();

                string result = ImportWindow.OpenOptionsWindow(
                    "Could not find class, please select which class to use",
                    oldClassData.Name,
                    closest.Select(data => data.Name).ToArray()
                );
                if (!string.IsNullOrEmpty(result))
                {
                    ClassData found = closest.First(data => data.Name == result);


//                        oldData.IndexOf(found) // todo save the new chosen value in the list, and hope that works :o

                    return found;
                }

                Debug.LogError("[Data loss] Could not find class for : " + oldClassData.Name +
                               " and no new class was chosen. This script will not be migrated!");

                return newFileData;
            }

            Debug.Log("Could not find old guid that matches the class with guid: " + oldGuid + " fileID : " +
                      oldFileID);
            return null;
        }
    }
}
#endif