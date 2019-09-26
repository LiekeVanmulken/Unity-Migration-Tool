#if UNITY_EDITOR
using ExtensionMethods;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
        /// Then checks all to be serialized fields and returns them in a list
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static List<FileData> ExportClassData(string path)
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
                EditorUtility.DisplayProgressBar("Exporting IDs", "Exporting IDs " + Path.GetFileName(file),
                    progress / totalFiles);
                var lines = File.ReadAllLines(file);

                foreach (string line in lines)
                {
                    Regex regex = new Regex(@"(?<=guid: )[A-z0-9]*");
                    Match match = regex.Match(line);
                    if (match.Success)
                    {
                        string className = getTypeByMetafileFileName(file);
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
                EditorUtility.DisplayProgressBar("Exporting IDs", "Exporting IDs " + Path.GetFileName(metaFile),
                    progress / totalFiles);
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
                        EditorUtility.DisplayProgressBar("Exporting IDs", "Exporting IDs " + type,
                            progress / totalFiles);
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
        public static string[] ImportClassDataAndTransformIDsInScene(string fileToChange, List<FileData> oldIDs,
            List<FileData> newIDs = null)
        {
            EditorUtility.DisplayProgressBar("Import progress bar", "Importing progress bar.", 0.5f);
            if (oldIDs == null)
            {
                throw new NotImplementedException("ExistingData is null");
            }

            var currentIDs = newIDs ?? ExportClassData(Application.dataPath);
            var linesToChange = File.ReadAllLines(fileToChange);

            linesToChange = MigrateGUIDsAndFieldIDs(linesToChange, currentIDs, oldIDs);
            EditorUtility.ClearProgressBar();

            return linesToChange;
        }


        /// <summary>
        /// Replaces the GUID and fileID, matching the oldIDs with the currentIDs
        /// </summary>
        /// <param name="linesToChange"></param>
        /// <param name="currentIDs">List of GUIDs and FileID for all currently in the project classes.</param>
        /// <param name="oldIDs">List of GUIDs and FileID for all classes in the previous project.</param>
        /// <returns></returns>
        private static string[] MigrateGUIDsAndFieldIDs(string[] linesToChange, List<FileData> currentIDs,
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


                // Get the new value with by matching the old id with the one in the file and finding the matching class in the new IDs
                FileData replacementFileData = findNewID(oldIDs, currentIDs, fileID, matchGuid.Value);
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

        public static List<FoundScript> FindFieldsToMigrate(string[] linesToChange, List<FileData> currentIDs)
        {
            EditorUtility.DisplayProgressBar("Field Migration", "Finding fields to migrate.", 0.5f);
            List<FoundScript> generateFieldMapping = GenerateFieldMapping(linesToChange, currentIDs);
            EditorUtility.ClearProgressBar();

            return generateFieldMapping;
        }

//        /// <summary>
//        /// Test method to call the migrateFieldData without calling the rest
//        /// </summary>
//        /// <param name="path"></param>
//        /// <param name="currentIDS"></param>
//        /// <returns></returns>
//        public static string[] TestVariableMapping(string path, List<FileData> currentIDS)
//        {
//            string[] lines = File.ReadAllLines(path);
//            return MigrateFieldNames(lines, currentIDS);
//        }

        /// <summary>
        /// Helper method to change the fields to the corresponding new name
        /// </summary>
        /// <param name="linesToChange"></param>
        /// <param name="currentIDs"></param>
        /// <returns></returns>
        private static List<FoundScript> GenerateFieldMapping(string[] linesToChange, List<FileData> currentIDs)
        {
            string content = string.Join("\n", linesToChange);

            YamlStream yamlStream = new YamlStream();
            yamlStream.Load(new StringReader(content));

            List<FoundScript> foundScripts = new List<FoundScript>();
            
            for (var i = 0; i < yamlStream.Documents.Count; i++)
            {
                YamlDocument document = yamlStream.Documents[i];

                //Only change it if it's a MonoBehaviour as unity script won't be as easily broken
                string type = document.GetName();
                if (type != "MonoBehaviour")
                {
                    continue;
                }

                YamlNode script = document.RootNode.GetChildren()["MonoBehaviour"];

                string fileID = (string) script["m_Script"]["fileID"];
                string guid = (string) script["m_Script"]["guid"];

                //    get corresponding fileData
                FileData currentFileData = currentIDs.First(data => data.FileID == fileID && data.Guid == guid);

//                linesToChange = findFieldMappings(linesToChange, script, currentFileData.FieldDatas);
                foundScripts.Add(new FoundScript(currentFileData, script));

                
            }
            return foundScripts;
        }

        [Serializable]
        public class FoundScript
        {
            public FileData fileData;
            public YamlNode yamlOptions;
            public bool HasBeenMapped;
            
            public FoundScript()
            {
            }

            public FoundScript(FileData fileData, YamlNode yamlOptions)
            {
                this.fileData = fileData;
                this.yamlOptions = yamlOptions;
                this.HasBeenMapped = hasBeenMapped(fileData.FieldDatas, yamlOptions);

            }

            private bool hasBeenMapped(FieldData[] datas, YamlNode node)
            {
                IDictionary<YamlNode, YamlNode> possibilities = node.GetChildren();
                foreach (FieldData fieldData in datas)
                {
                    KeyValuePair<YamlNode, YamlNode> found = possibilities.FirstOrDefault(pos => (string)pos.Key == fieldData.Name); //todo : check if this works
                    if (found.Key == null)//todo : check if this works
                    {
                        return false;
                    }
                    if (!hasBeenMapped(fieldData.Children, node[found.Key]))
                    {
                        return false;
                    }
                }
                return true;

            }

        }

//        private static List<FoundField> findFieldMappings(string[] linesToChange, YamlNode script, FileData fileData)
//        {
//            List<FoundField> foundFields = new List<FoundField>();
//            
//            //Get all fields in the document of the script in the scene file
////            List<string> yamlFields = script.GetChildren().Select(pair => pair.Key.ToString()).ToList();
//
//
////            foreach (FieldData member in unmapped)
////            {
////                var found = yamlFields.FirstOrDefault(yamlMember => member.Name == yamlMember);
//
//                foundFields.Add(new FoundField(fileData, script));
////            }
//
//
//            return linesToChange;
//        }

        #region old map data

//        private static string[] MapFields(string[] linesToChange, YamlNode script, MemberData[] members)
//        {
//            List<MemberData> unmapped = new List<MemberData>();
//            IDictionary<YamlNode, YamlNode> sceneFileMembers = script.GetChildren();
//
//            // check if all fields are present
//            foreach (MemberData member in members)
//            {
//                if (!sceneFileMembers.ContainsKey(member.Name))
//                {
//                    unmapped.Add(member);
//                }
//            }
//
//            //if not check and use a mapping
//            List<string> yamlMembers = script.GetChildren().Select(pair => pair.Key.ToString()).ToList();
//            List<string> replaced = new List<string>(); 
//            foreach (MemberData member in unmapped)
//            {
//                string closest = yamlMembers.OrderBy(yamlMember => Levenshtein.Compute(member.Name, yamlMember))
//                    .First();
//                if (replaced.Contains(closest))
//                {
//                    Debug.LogError("Tried to map " + closest + " to " + member.Name + " but it was already mapped");
//                    continue;
//                }
//                replaced.Add(closest);
//
//                var foundLine = script[closest].Start.Line - 1;
//                linesToChange[foundLine] = linesToChange[foundLine].ReplaceFirst(closest, member.Name);
//
//                Debug.LogWarning("Replaced fieldName: " + closest + " with " + member.Name + " on line " + foundLine);
//            }
//
//            //Replace for all subobjects
//            foreach (var member in members)
//            {
//                if (member.Children != null && member.Children.Length > 0)
//                {
//                    string closest = yamlMembers.OrderBy(yamlMember => Levenshtein.Compute(member.Name, yamlMember))
//                        .First();
//                    linesToChange = MapFields(linesToChange, script[closest], member.Children);
//                }
//            }
//
//            return linesToChange;
//        }

        #endregion

        /// <summary>
        /// Get the Type of a class by the name of the class.
        /// </summary>
        /// <param name="path">The path of the meta file we're getting the name from</param>
        /// <returns></returns>
        private static string getTypeByMetafileFileName(string path)
        {
            string fileName = Path.GetFileName(path);
            fileName = fileName.Replace(".cs.meta", "");
            Type[] types = assemblies.SelectMany(x => x.GetTypes())
                .Where(x => x.Name == fileName).ToArray();

            if (types.Length == 0)
            {
                Debug.Log("Could not find type with name : " + fileName);
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

        /// <summary>
        /// Finds the new GUID and fileID from the old IDs and the new IDs by checking for the classname in both
        /// </summary>
        /// <param name="oldData"></param>
        /// <param name="newData"></param>
        /// <param name="fileId"></param>
        /// <param name="oldGuid"></param>
        /// <returns></returns>
        private static FileData findNewID(List<FileData> oldData, List<FileData> newData, string fileId,
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