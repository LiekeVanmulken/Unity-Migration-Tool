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
using static importerexporter.MergingWizard;

namespace importerexporter
{
    /// <summary>
    /// Imports and exports the guids and fileIDS from projects
    /// </summary>
    public class ImportExportUtility
    {
        #region Singleton

        private static ImportExportUtility instance = null;

        private static readonly object padlock = new object();

        ImportExportUtility()
        {
        }

        public static ImportExportUtility Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new ImportExportUtility();
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
            var classMetaFiles = Directory.GetFiles(path, "*" +
                                                          ".cs.meta", SearchOption.AllDirectories);
            //Get all dlls
            var dllMetaFiles = Directory.GetFiles(path, "*" +
                                                        ".dll.meta", SearchOption.AllDirectories);

            int totalFiles = classMetaFiles.Length + dllMetaFiles.Length;

            List<ClassData> data = new List<ClassData>();
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

                        data.Add(new ClassData(className, match.Value));
                    }
                }
            }

            // Loop through dlls  
            if (!constants.DEBUG)
            {
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
                            data.Add(new ClassData(type.FullName, match.Value, FileIDUtil.Compute(type).ToString()));
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("Could not load assembly : " + file + "\nException : " + e);
                    }
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
        public string[] ImportClassDataAndTransformIDs(string fileToChange, List<ClassData> oldIDs,
            List<ClassData> newIDs = null)
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
        private string[] MigrateGUIDsAndFieldIDs(string[] linesToChange, List<ClassData> currentIDs,
            List<ClassData> oldIDs)
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
                ClassData replacementClassData = findNewID(oldIDs, currentIDs, fileID, matchGuid.Value);
                if (replacementClassData == null)
                {
                    continue;
                }

                // Replace the Guid
                linesToChange[i] = linesToChange[i].Replace(matchGuid.Value, replacementClassData.Guid);

                if (String.IsNullOrEmpty(fileID)) continue;

                //Replace the fileID
                linesToChange[i] = linesToChange[i].Replace(fileID, replacementClassData.FileID);
            }

            return linesToChange;
        }

        /// <summary>
        /// Finds all fields that need to be migrated from the yaml
        /// </summary>
        /// <param name="linesToChange"></param>
        /// <param name="currentIDs"></param>
        /// <returns></returns>
        public List<FoundScript> FindFieldsToMigrate(string[] linesToChange, List<ClassData> currentIDs)
        {
            EditorUtility.DisplayProgressBar("Field Migration", "Finding fields to migrate.", 0.5f);
            List<FoundScript> generateFieldMapping = GenerateFieldMapping(linesToChange, currentIDs);
            EditorUtility.ClearProgressBar();

            return generateFieldMapping;
        }

        /// <summary>
        /// Helper method to change the fields to the corresponding new name
        /// </summary>
        /// <param name="linesToChange"></param>
        /// <param name="currentIDs"></param>
        /// <returns></returns>
        private List<FoundScript> GenerateFieldMapping(string[] linesToChange, List<ClassData> currentIDs)
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

                //    get corresponding classData
                ClassData currentClassData = currentIDs.First(data => data.FileID == fileID && data.Guid == guid);
                FoundScript found = new FoundScript(currentClassData, script);
                if (!found.HasBeenMapped)
                {
                    foundScripts.Add(found);
                }
            }

            return foundScripts;
        }

        /// <summary>
        /// Replaces the Fields on the monobehaviours according to the mergeNode data
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="foundScripts"></param>
        /// <returns></returns>
        public string[]
            ReplaceFieldsByMergeNodes(string[] scene, List<FoundScript> foundScripts) //todo : this needs a new name!
        {
            string sceneContent = string.Join("\n", scene);

            YamlStream yamlStream = new YamlStream();
            yamlStream.Load(new StringReader(sceneContent));
            List<YamlDocument> yamlDocuments =
                yamlStream.Documents.Where(document => document.GetName() == "MonoBehaviour").ToList();
            foreach (YamlDocument document in yamlDocuments)
            {
                YamlNode script = document.RootNode.GetChildren()["MonoBehaviour"]; //todo : duplicate code, fix 

                string fileID = (string) script["m_Script"]["fileID"];
                string guid = (string) script["m_Script"]["guid"];

                FoundScript scriptType =
                    foundScripts.First(node =>
                        node.classData.Guid == guid && node.classData.FileID == fileID);
                scene = recursiveReplaceField(scene, scriptType.MergeNodes, script);
            }

            return scene;
        }

        /// <summary>
        /// Class used to return the scripts that need to be remapped by the user.
        /// When the fielnames in the yaml do not match the fields in the class.
        /// The reason this is reaturnes is because this needs to be filled in by the user and as such
        /// cannot be done in the importExportUtility as this needs user input.
        ///
        /// Used in the <see cref="ImportExportUtility.GenerateFieldMapping"/> and the <seealso cref="MergingWizard"/>
        /// </summary>
        [Serializable]
        public class FoundScript
        {
            public ClassData classData;
            public YamlNode yamlOptions;
            public bool HasBeenMapped;
            public List<MergeNode> MergeNodes = new List<MergeNode>();

            public FoundScript()
            {
            }

            public FoundScript(ClassData classData, YamlNode yamlOptions)
            {
                this.classData = classData;
                this.yamlOptions = yamlOptions;
                this.HasBeenMapped = hasBeenMapped(classData.FieldDatas, yamlOptions);
            }

            /// <summary>
            /// Checks if the field has a exact match between the yaml and the classField
            /// </summary>
            /// <param name="datas"></param>
            /// <param name="node"></param>
            /// <returns></returns>
            private bool hasBeenMapped(FieldData[] datas, YamlNode node)
            {
                IDictionary<YamlNode, YamlNode> possibilities = node.GetChildren();
                foreach (FieldData fieldData in datas)
                {
                    KeyValuePair<YamlNode, YamlNode> found =
                        possibilities.FirstOrDefault(pos =>
                            (string) pos.Key == fieldData.Name); //todo : check if this works
                    if (found.Key == null) //todo : check if this works
                    {
                        return false;
                    }

                    if (fieldData.Children != null && !hasBeenMapped(fieldData.Children, node[found.Key]))
                    {
                        return false;
                    }
                }

                return true;
            }
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
        private ClassData findNewID(List<ClassData> oldData, List<ClassData> newData, string fileId,
            string oldGuid)
        {
            ClassData oldClassData = null;
            foreach (ClassData currentOldFileData in oldData)
            {
                if ((currentOldFileData.Guid.Equals(oldGuid) && string.IsNullOrEmpty(fileId))
                    || (currentOldFileData.Guid.Equals(oldGuid) && currentOldFileData.FileID.Equals(fileId)))
                {
                    oldClassData = currentOldFileData;
                    break;
                }
            }

            if (oldClassData != null)
            {
                var newFileData = newData.First(filedata => filedata.Name.Equals(oldClassData.Name));
                return newFileData;
            }

            Debug.Log("Could not find old guid that matches the class with guid: " + oldGuid + " fileID : " + fileId);
            return null;
        }


        /// <summary>
        /// Helper method for the<see cref="ReplaceFieldsByMergeNodes"/> to replace the fields in the scripts.
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="currentMergeNodes"></param>
        /// <param name="rootYamlNode"></param>
        /// <returns></returns>
        private string[] recursiveReplaceField(string[] scene, List<MergeNode> currentMergeNodes,
            YamlNode rootYamlNode)
        {
            IDictionary<YamlNode, YamlNode> yamlChildren = rootYamlNode.GetChildren();
            foreach (KeyValuePair<YamlNode, YamlNode> yamlNode in yamlChildren)
            {
                string yamlNodeKey = (string) yamlNode.Key;
                int line = yamlNode.Key.Start.Line - 1;
                var currentMergeNode = currentMergeNodes.First(node => node.YamlKey == yamlNodeKey);

                if (!string.IsNullOrEmpty(currentMergeNode.ValueToExportTo))
                {
                    scene[line] = scene[line].ReplaceFirst(currentMergeNode.YamlKey, currentMergeNode.ValueToExportTo);
                }

                if (yamlNode.Value is YamlMappingNode &&
                    !constants.MonoBehaviourFieldExclusionList.Contains((string) yamlNode.Key))
                {
                    var recursiveChildren = yamlNode.Value.GetChildren();
                    if (recursiveChildren == null || recursiveChildren.Count == 0)
                    {
                        continue;
                    }

                    recursiveReplaceField(scene, currentMergeNode.MergeNodes, yamlNode.Value);
                }
            }

            return scene;
        }
    }
}
#endif