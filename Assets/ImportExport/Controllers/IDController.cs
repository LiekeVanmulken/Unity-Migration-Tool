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

namespace importerexporter.controllers
{
    /// <summary>
    /// Imports and exports the guids and fileIDS from projects
    /// </summary>
    public class IDController : Singleton<IDController>
    {
        /// <summary>
        /// Reference to the constants 
        /// </summary>
        private Constants constants = Constants.Instance;

        /// <summary>
        /// Cached field of all assemblies to loop through
        /// </summary>
        private Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        /// <summary>
        /// Cache of a array of all assembly types with the fullname lowered.
        /// This is to prevent the types being called often and the name being lowered in the <see cref="getTypeByMetafileFileName"/>
        /// </summary>
        private KeyValuePair<string, Type>[] cachedLowerTypeList;


        /// <summary>
        /// Gets all the classes in the project and gets the name of the class, the guid that unity assigned and the fileID.
        /// Then checks all to be serialized fields and returns them in a list
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public List<ClassModel> ExportClassData(string path)
        {
            //Get all meta files
            string[] classMetaFiles = Directory.GetFiles(path, "*.cs.meta", SearchOption.AllDirectories);

            //Get all dlls
            string[] dllMetaFiles = Directory.GetFiles(path, "*.dll.meta", SearchOption.AllDirectories);

            int totalFiles = classMetaFiles.Length + dllMetaFiles.Length;

            int gcCount = 0;
            int gcLimit = 50000;
            List<ClassModel> classes = new List<ClassModel>();
            for (var i = 0; i < classMetaFiles.Length; i++)
            {
                string file = classMetaFiles[i];
                ParseSourceFile((float)i/classMetaFiles.Length, file, ref classes, gcLimit, ref gcCount);
            }

            // Loop through dlls  
            if (!constants.DEBUG)
            {
//                Thread[] dllThreads = new Thread[dllMetaFiles.Length];
                for (var i = 0; i < dllMetaFiles.Length; i++)
                {
                    string metaFile = dllMetaFiles[i];
//                    dllThreads[i] =
//                        new Thread(() => 
                     ParseDLLFile((float)i/dllMetaFiles.Length, metaFile, ref classes, gcLimit, ref gcCount)
//                                )
                        ;
//                    dllThreads[i].Start();
                }

//                bool stillRunning = true;
//                while (stillRunning)
//                {
//                    int threadsRunning = dllThreads.Where(thread => thread.IsAlive).ToArray().Length;
//                    stillRunning = threadsRunning > 0;
//
//                    MigrationWindow.DisplayProgressBar("Exporting IDs from DLLs",
//                        "Threads running " + threadsRunning + "/" + dllThreads.Length,
//                        threadsRunning / dllThreads.Length);
//                    Thread.Sleep(100);
//                }
            }
            
            MigrationWindow.ClearProgressBar();
            return classes;
        }
    
        private void ParseSourceFile(float progress, string file, ref List<ClassModel> data,
            int gcLimit,
            ref int gcCount)
        {
            MigrationWindow.DisplayProgressBar("Exporting IDs", "Exporting IDs " + Path.GetFileName(file),
                progress );
            IEnumerable<string> lines = File.ReadLines(file);

            foreach (string line in lines)
            {
                Match match = constants.RegexGuid.Match(line);
                if (!match.Success) continue;

                string className = getTypeByMetafileFileName(file);
                if (String.IsNullOrEmpty(className))
                {
                    continue;
                }

                data.Add(new ClassModel(className, match.Value));
                break;
            }

            gcCount++;
            if (gcCount > gcLimit)
            {
                GC.Collect();
            }

        }

        private void ParseDLLFile(float progress, string metaFile, ref List<ClassModel> data,
            int gcLimit,
            ref int gcCount)
        {
            MigrationWindow.DisplayProgressBar("Exporting IDs", "Exporting IDs " + Path.GetFileName(metaFile),
                progress);
            string text = File.ReadAllText(metaFile);
            Match match = constants.RegexGuid.Match(text);
            if (!match.Success)
            {
                Debug.LogError("Could not parse the guid from the dll meta file. File : " +
                               metaFile);
            }

            string file = metaFile.Replace(".meta", "");
            try
            {
                if (Path.GetFileName(file).Contains("Newtonsoft.Json") ||
                    Path.GetFileName(file).Contains("YamlDotNet"))
                {
                    return;
                }

                Assembly assembly = Assembly.LoadFile(file);
                foreach (Type type in assembly.GetTypes())
                {
                    MigrationWindow.DisplayProgressBar("Exporting IDs", "Exporting IDs " + type,progress);
                    data.Add(new ClassModel(type, match.Value, FileIDUtil.Compute(type).ToString()));


                    gcCount++;
                    if (gcCount > gcLimit)
                    {
                        GC.Collect();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Could not load assembly : " + file + "\nException : " + e);
            }
        }

        /// <summary>
        /// Replaces all old GUIDs and old fileIDs with the new GUID and fileID and returns a the new scenefile.
        /// This can be saved as an .unity file and then be opened in the editor.
        /// </summary>
        /// <param name="fileToChange"></param>
        /// <param name="oldIDs"></param>
        /// <param name="newIDs"></param>
        /// <param name="foundScripts"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public string[] TransformIDs(string fileToChange, List<ClassModel> oldIDs,
            List<ClassModel> newIDs, ref List<FoundScript> foundScripts)
        {
            MigrationWindow.DisplayProgressBar("Migration started",
                "Start importing current project classData and migrating scene.", 0.5f);
            if (oldIDs == null || newIDs == null || foundScripts == null)
            {
                throw new NullReferenceException("Some of the data with which to export is null.");
            }

            string[] linesToChange = File.ReadAllLines(fileToChange);

            linesToChange = MigrateGUIDsAndFieldIDs(linesToChange, oldIDs, newIDs, ref foundScripts);
            MigrationWindow.ClearProgressBar();

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
        private string[] MigrateGUIDsAndFieldIDs(string[] linesToChange, List<ClassModel> oldIDs,
            List<ClassModel> newIDs,
            ref List<FoundScript> foundScripts)
        {
            string sceneContent = string.Join("\r\n", linesToChange);

            YamlStream yamlStream = new YamlStream();
            yamlStream.Load(new StringReader(sceneContent));
            IEnumerable<YamlDocument> yamlDocuments =
                yamlStream.Documents.Where(document => document.GetName() == "MonoBehaviour");
            foreach (YamlDocument document in yamlDocuments)
            {
                YamlNode monoBehaviour = document.RootNode.GetChildren()["MonoBehaviour"];

                YamlNode oldFileIdNode = monoBehaviour["m_Script"]["fileID"];
                YamlNode oldGuidNode = monoBehaviour["m_Script"]["guid"];

                string oldFileId = oldFileIdNode.ToString();
                string oldGuid = oldGuidNode.ToString();

                ClassModel oldClassModel =
                    oldIDs.FirstOrDefault(data =>
                        data.Guid == oldGuid && data.FileID == oldFileId);
                if (oldClassModel == null)
                {
                    Debug.LogError("Could not find class for script with type, not migrating guid : " + oldGuid +
                                   " oldFileID : " + oldFileId);
                    continue;
                }

                FoundScript
                    mapping = FoundScriptMappingRecursively(newIDs, ref foundScripts,
                        oldClassModel);
                if (mapping == null)
                {
                    Debug.LogError("mapping is null for " + oldClassModel.FullName);
                    continue;
                }

                int line = oldFileIdNode.Start.Line - 1;
                if (!string.IsNullOrEmpty(mapping.newClassModel.Guid))
                {
                    // Replace the Guid
                    linesToChange[line] = linesToChange[line].ReplaceFirst(oldGuid, mapping.newClassModel.Guid);
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
                    linesToChange[line] = linesToChange[line].ReplaceFirst(oldFileId, mapping.newClassModel.FileID);
                }
            }

            return linesToChange;
        }

        /// <summary>
        /// Maps all foundScripts for all variables and children of the type of the variable
        /// </summary>
        /// <param name="newIDs">The IDs of the new project</param>
        /// <param name="foundScripts">The existing foundScripts that will be looked in and added to</param>
        /// <param name="oldClassModel">Current class data of the old project as this maps to the scene file</param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        private FoundScript FoundScriptMappingRecursively(List<ClassModel> newIDs,
            ref List<FoundScript> foundScripts, ClassModel oldClassModel)
        {
            if (oldClassModel == null)
            {
                throw new NullReferenceException("No old classData found");
            }

            FoundScript existingFoundScript = foundScripts.FirstOrDefault(script =>
                script.oldClassModel.FullName == oldClassModel.FullName);

            ClassModel replacementClassModel =
                existingFoundScript
                    ?.newClassModel;
            if (replacementClassModel == null && oldClassModel.Fields != null)
            {
                replacementClassModel = findNewID(newIDs, oldClassModel); //todo : testScript gets called double
                if (replacementClassModel == null)
                {
                    return null;
                }
            }
            else if (replacementClassModel != null)
            {
                return existingFoundScript;
            }
            else
            {
                return null;
            }

            if (existingFoundScript != null)
            {
                return existingFoundScript;
            }


            existingFoundScript = new FoundScript
            {
                oldClassModel = oldClassModel,
                newClassModel = replacementClassModel
            };
            MappedState hasBeenMapped = existingFoundScript.CheckHasBeenMapped();
            if (hasBeenMapped == MappedState.NotMapped)
            {
                existingFoundScript.GenerateMappingNode(foundScripts);
            }

            foundScripts.Add(existingFoundScript);

            //If it doesn't exist then create it

            if (oldClassModel.Fields != null && oldClassModel.Fields.Length != 0)
            {
                foreach (FieldModel field in oldClassModel.Fields)
                {
                    // Check if the type is null as this would cause so errors in the mapping
                    if (field.Type == null)
                    {
                        throw new NullReferenceException("type of field is null for some reason");
                    }

                    FoundScriptMappingRecursively(newIDs, ref foundScripts, field.Type);
                }
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


            if (cachedLowerTypeList == null)
            {
                cachedLowerTypeList = assemblies.SelectMany(assembly => assembly.GetTypes())
                    .Select(type => new KeyValuePair<string, Type>(type.Name.ToLower(), type)).ToArray();
            }

            Type[] types = cachedLowerTypeList.Where(loweredTypeName => loweredTypeName.Key == fileNameLower)
                .Select(pair => pair.Value).ToArray();

            if (types.Length == 0)
            {
                Debug.Log("Checked for type  \"" + fileName +
                          "\" no types were found.");
                return null;
            }

            if (types.Length == 1)
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
            return MigrationWindow.OpenOptionsWindow("Class cannot be found, select which one to choose", fileName,
                options);
        }

        /// <summary>
        /// Finds the new GUID and fileID from the old IDs and the new IDs by checking for the classname in both
        /// </summary>
        /// <param name="newIDs"></param>
        /// <param name="old"></param>
        /// <returns></returns>
        private ClassModel findNewID(List<ClassModel> newIDs, ClassModel old)
        {
            if (old == null)
            {
                throw new NullReferenceException("Old ClassData cannot be null in the findNewID");
            }

            // Check if there is an exact match
            ClassModel newFileModel = newIDs.FirstOrDefault(data => data.FullName.Equals(old.FullName));
            if (newFileModel != null) return newFileModel;

            //Get all classes including all subclasses and check if it might be a subclass
            Dictionary<string, ClassModel> allClassData = generateOptions(newIDs);
            if (allClassData.ContainsKey(old.Name))
            {
                return allClassData[old.Name];
            }

            // Check if there is an exact match with only the classname
            ClassModel[] classModels = allClassData.Select(pair => pair.Value)
                .Where(model => model.NameLower == old.NameLower).ToArray();
            if (classModels.Length == 1)
            {
                return classModels[0];
            }

            // Generate the options for the options window
            string[] options = allClassData.Select(pair => pair.Key)
                .OrderBy(name => Levenshtein.Compute(name, old.FullName)).ToArray();

            // Open the options window
            string result = MigrationWindow.OpenOptionsWindow(
                "Could not find class, please select which class to use",
                old.FullName,
                options
            );

            // Return the selected class
            if (string.IsNullOrEmpty(result))
            {
                Debug.LogError("[Data loss] Could not find class for : " + old.FullName +
                               " and no new class was chosen. This script will not be migrated!");

                return null;
            }

            return allClassData[result];
        }

        /// <summary>
        /// Generate the options of all Classes for the options window
        /// </summary>
        /// <param name="allIDs"></param>
        /// <returns></returns>
        private Dictionary<string, ClassModel> generateOptions(List<ClassModel> allIDs)
        {
            Dictionary<string, ClassModel> dictionary = new Dictionary<string, ClassModel>();
            foreach (ClassModel id in allIDs)
            {
                generateOptionsRecursive(id, ref dictionary);
            }

            return dictionary;
        }

        /// <summary>
        /// Recursively find all classes that are being used in old project to generate a choice menu for the options window
        /// </summary>
        /// <param name="id"></param>
        /// <param name="dictionary"></param>
        private void generateOptionsRecursive(ClassModel id, ref Dictionary<string, ClassModel> dictionary)
        {
            if (string.IsNullOrEmpty(id.FullName))
            {
                return;
            }

            if (!dictionary.ContainsKey(id.FullName))
            {
                dictionary[id.FullName] = id;
            }
            else
            {
                //Compile all information
                if (dictionary[id.FullName].Fields == null)
                {
                    dictionary[id.FullName].Fields = id.Fields;
                }

                if (dictionary[id.FullName].Guid == null)
                {
                    dictionary[id.FullName].Guid = id.Guid;
                    dictionary[id.FullName].FileID = id.FileID;
                }
            }

            if (!dictionary.ContainsKey(id.FullName) ||
                ((string.IsNullOrEmpty(dictionary[id.FullName].Guid) && dictionary[id.FullName].Fields == null) ||
                 dictionary[id.FullName].Fields == null)
            )
            {
                dictionary[id.FullName] = id;
            }

            if (id.Fields == null || id.Fields.Length == 0)
            {
                return;
            }

            foreach (FieldModel field in id.Fields)
            {
                if (field.Type == null)
                {
                    continue;
                }

                if (field.Type.FullName != id.FullName)
                {
                    generateOptionsRecursive(field.Type, ref dictionary);
                }
            }
        }
    }
}
#endif