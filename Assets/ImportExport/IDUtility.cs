#if UNITY_EDITOR
using static importerexporter.models.FoundScript;
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
//        private readonly Regex regexFileID = new Regex(@"(?<=fileID: )\-?[A-z0-9]*");

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
            string[] classMetaFiles = Directory.GetFiles(path, "*.cs.meta", SearchOption.AllDirectories);
            //Get all dlls
            string[] dllMetaFiles = Directory.GetFiles(path, "*.dll.meta", SearchOption.AllDirectories);

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

                    string dllGuid = match.Value;
                    string file = metaFile.Replace(".meta", "");
                    try
                    {
                        Assembly assembly = Assembly.LoadFile(file);
                        foreach (Type type in assembly.GetTypes())
                        {
//                            if (!type.FullName.StartsWith("u040"))
//                            {
//                                continue;
//                            }
                            ImportWindow.DisplayProgressBar("Exporting IDs", "Exporting IDs " + type,
                                progress / totalFiles);
                            data.Add(new ClassData(type.FullName, dllGuid, FileIDUtil.Compute(type).ToString()));
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
            string sceneContent = string.Join("\r\n", linesToChange);

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

                ClassData oldClassData =
                    oldIDs.FirstOrDefault(data =>
                        data.Guid == oldGuid && data.FileID == oldFileId); // todo : this breaks
                if (oldClassData == null)
                {
                    Debug.LogError("Could not find class for script with type, not migrating guid : " + oldGuid +
                                   " oldFileID : " + oldFileId);
                    continue;
                }

                if (oldClassData.Name == "u040.prespective.prepair.physics.kinetics.BeltSystem")
                {
                    Debug.Log("test");
                }

                FoundScript
                    mapping = RecursiveFoundScriptTest(newIDs, ref foundScripts,
                        oldClassData);
                if (mapping == null)
                {
                    Debug.LogError("mapping is null, really check!!!!" + oldGuid + " - " + oldFileId);
                    continue;
//                    throw new NotImplementedException("Mapping is null");
                }

                int line = oldFileIdNode.Start.Line - 1;

                if (!string.IsNullOrEmpty(mapping.NewClassData.Guid))
                {
                    // Replace the Guid
                    linesToChange[line] = linesToChange[line].ReplaceFirst(oldGuid, mapping.NewClassData.Guid);
                }
                else
                {
                    Debug.Log("Found empty guid");
                    continue;
                    //todo : this should throw an error
                    //todo : this is when a non script is being used or the guid is not available. This should probably be a popup with a warning
                }


                if (!String.IsNullOrEmpty(oldFileId))
                {
                    linesToChange[line] = linesToChange[line].ReplaceFirst(oldFileId, mapping.NewClassData.FileID);
                }


                //Replace the fileID
            }

            return linesToChange;
        }

        private FoundScript RecursiveFoundScriptTest(List<ClassData> newIDs, // todo
            ref List<FoundScript> foundScripts, ClassData oldClassData)
        {
            if (oldClassData == null)
            {
                throw new NotImplementedException("No old classData found");
            }

            if (oldClassData.Name == "u040.prespective.prepair.physics.kinetics.BeltSystem")
            {
                Debug.Log("test beltSystem");
            }

            FoundScript existingFoundScript = foundScripts.FirstOrDefault(script =>
                script.OldClassData.Name == oldClassData.Name);

            ClassData replacementClassData =
                existingFoundScript
                    ?.NewClassData;
            if (replacementClassData == null && oldClassData.Fields != null && oldClassData.Fields?.Length != 0)
            {
                replacementClassData = findNewID(newIDs, oldClassData);
            }
            else if (replacementClassData != null)
            {
                return existingFoundScript;
            }
            else
            {
                return null;
            }

            if (existingFoundScript == null)
            {
                if (oldClassData.Fields != null && oldClassData.Fields.Length != 0)
                {
                    foreach (FieldData field in oldClassData.Fields)
                    {
                        if (field.Type == null)
                        {
                            throw new NotImplementedException("type of field is null for some reason");
                        } //todo : check if already exists 

                        RecursiveFoundScriptTest(newIDs, ref foundScripts, field.Type);
                    }
                }

                existingFoundScript = new FoundScript
                {
                    OldClassData = oldClassData,
                    NewClassData = replacementClassData
                };
                MappedState hasBeenMapped = existingFoundScript.CheckHasBeenMapped();
                if (hasBeenMapped == MappedState.NotMapped)
                {
                    existingFoundScript.GenerateMappingNode(foundScripts);
                }


                foundScripts.Add(existingFoundScript);
            }

            return existingFoundScript;
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
        /// <param name="newIDs"></param>
        /// <param name="old"></param>
        /// <returns></returns>
        public ClassData
            findNewID(List<ClassData> newIDs,
                ClassData old) // todo : check if the classname is the same but not the namespace
        {
            if (old == null)
            {
                throw new NullReferenceException("Old ClassData cannot be null in the findNewID");
            }

            ClassData newFileData = newIDs.FirstOrDefault(filedata => filedata.Name.Equals(old.Name));
            if (newFileData != null) return newFileData;


            Dictionary<string, ClassData> allClassData = generateOptions(newIDs);
            string[] options = allClassData.Select(pair => pair.Key)
                .OrderBy(name => Levenshtein.Compute(name, old.Name)).ToArray();

//            ClassData[] ordered = newIDs
//                .OrderByDescending(data => Levenshtein.Compute(data.Name, old.Name))
//                .ToArray();

            string result = ImportWindow.OpenOptionsWindow(
                "Could not find class, please select which class to use",
                old.Name,
                options
            );

            if (string.IsNullOrEmpty(result))
            {
                Debug.LogError("[Data loss] Could not find class for : " + old.Name +
                               " and no new class was chosen. This script will not be migrated!");

                return newFileData; // todo : why is this always null
            }

            var foundClassData = allClassData.Where(pair => pair.Key == result).ToArray();
            switch (foundClassData.Length)
            {
                case 0:
                    throw new NullReferenceException("Cannot find selected class");
                case 1:
                    return foundClassData[0].Value;
                default:
                    return foundClassData.First(pair => !string.IsNullOrEmpty(pair.Value.Guid)).Value;
            }
        }

        private Dictionary<string, ClassData> generateOptions(List<ClassData> allIDs)
        {
            Dictionary<string, ClassData> dictionary = new Dictionary<string, ClassData>();
            foreach (ClassData id in allIDs)
            {
                generateOptionsRecursive(id, ref dictionary);
            }

            return dictionary;
        }

        private void generateOptionsRecursive(ClassData id, ref Dictionary<string, ClassData> dictionary)
        {
            if (string.IsNullOrEmpty(id.Name))
            {
                Debug.LogError("id.name is null in the generateOptionsRecursive");
                return;
            }

            if (!dictionary.ContainsKey(id.Name) || string.IsNullOrEmpty(dictionary[id.Name].Guid))
            {
                dictionary[id.Name] = id;
            }

            if (id.Fields == null || id.Fields.Length == 0)
            {
                return;
            }

            foreach (FieldData field in id.Fields)
            {
                if (field.Type == null)
                {
                    continue;
                }

                generateOptionsRecursive(field.Type, ref dictionary);
            }
        }
    }
}
#endif