using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using migrationtool.models;
using migrationtool.utility;
using migrationtool.views;
using migrationtool.windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using SceneView = migrationtool.views.SceneView;
using Constants = migrationtool.utility.Constants;

namespace u040.prespective.migrationtool
{
    public class PREpackageImporter
    {
        private static Constants constants = Constants.Instance;
        private static Administration administration = Administration.Instance;

        private static SceneView sceneView = new SceneView();
        private static PrefabView prefabView = new PrefabView();
        private static IDExportView IDView = new IDExportView();


        [MenuItem("Assets/PREspective/Migrate To Latest Version")]
        private static void MigratePREspective()
        {
            MigratePREspective(null);
        }

        /// <summary>
        /// Import the
        /// </summary>
        /// <param name="version"></param>
        private static void MigratePREspective(string version = null)
        {
            if (!EditorUtility.DisplayDialog("BACKUP YOUR PROJECT!",
                "Please BACKUP your project before proceeding. A faulty migration can lead to DATA LOSS!",
                "I've made a backup!", "I will make a backup now"))
            {
                return;
            }

            if (!EditorUtility.DisplayDialog("THIS CAN IRREVERSIBLY BREAK YOUR PROJECT!",
                "This upgrade can IRREVERSIBLY break your project. Please really have a backup with all meta files.",
                "I've made a backup!", "I will make a backup now"))
            {
                return;
            }

            //todo : make a backup of the project 
            administration.ShowInfoPopups = false;
            EditorUtility.DisplayProgressBar("Migration starting", "Migration Starting", 0.2f);


            string packageLocation = getPackageLocation(version);
            if (string.IsNullOrEmpty(packageLocation))
            {
                Debug.Log("No package selected.");
                return;
            }

            string currentPREspectiveVersion = GetPREspectiveDLLVersion();
            PlayerPrefs.SetString(PrepackageConstants.PREPACKAGE_PACKAGE_LOCATION, packageLocation);
            PlayerPrefs.SetString(PrepackageConstants.PREPACKAGE_PACKAGE_VERSION_OLD, currentPREspectiveVersion);
            Debug.Log("Migrating from version " + currentPREspectiveVersion);

            ThreadUtil.RunThread(() =>
            {
                packageImportStarted(constants.RootDirectory, packageLocation);
                List<string> files = Unzipper.ParseUnityPackagesToFiles(packageLocation);
                string packageContent = string.Join(",", files);
                ThreadUtil.RunMainThread(() =>
                {
                    AssetDatabase.ImportPackage(packageLocation, true);
                    PlayerPrefs.SetString(PrepackageConstants.PREPACKAGE_PACKAGE_LOCATION, packageLocation);
                    PlayerPrefs.SetString(PrepackageConstants.PREPACKAGE_PACKAGE_CONTENT, packageContent);
                });
            });
        }

        /// <summary>
        /// Get the package for the latest or wanted version
        /// </summary>
        /// <param name="wantedVersion"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        private static string getPackageLocation(string wantedVersion = null)
        {
            EditorUtility.DisplayProgressBar("Migration starting", "Downloading versions", 0.3f);

            WebClient client = new WebClient();
            using (MemoryStream stream =
                new MemoryStream(client.DownloadData(PrepackageConstants.PREPACKAGE_DOMAIN +
                                                     PrepackageConstants.PREPACKAGE_VERSIONS_PATH)))
            {
                string request = Encoding.ASCII.GetString(stream.ToArray());
                Debug.LogError(request);
                JArray packageVersions = JArray.Parse(request);

                var versionToUse = packageVersions[packageVersions.Count - 1];
                string packageUrl = (string) versionToUse["packageUrl"];
                string version = (string) versionToUse["version"];

                if (wantedVersion != null && version != wantedVersion)
                {
                    bool hasBeenSet = false;
                    foreach (JObject packageVersion in packageVersions)
                    {
                        string innerVersion = (string) packageVersion["version"];
                        if (innerVersion != wantedVersion) continue;

                        version = innerVersion;
                        packageUrl = (string) packageVersion["packageUrl"];
                        hasBeenSet = true;
                        break;
                    }

                    if (!hasBeenSet)
                    {
                        throw new FormatException("Could not find the package version. Version: v" + wantedVersion);
                    }
                }

                string packageLocal = Directory.GetCurrentDirectory() + "/PREspectivePackages/PREspective_v" + version +
                                      ".prepackage";
                if (!Directory.Exists(Path.GetDirectoryName(packageLocal)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(packageLocal));
                }

                client.DownloadFile(packageUrl, packageLocal);
                PlayerPrefs.SetString(PrepackageConstants.PREPACKAGE_PACKAGE_VERSION_NEW, version);
                return packageLocal;
            }
        }


        /// <summary>
        /// Runs after the package has been imported
        /// </summary>
        /// <param name="projectPath"></param>
        /// <param name="packageLocation"></param>
        public static void packageImportFinished(string projectPath, string packageLocation)
        {
            Debug.LogWarning("[PREpackage] Project migration started ");

            Constants constants = Constants.Instance;

            string customOldIDSPath = projectPath + constants.RelativeExportPath;
            if (!File.Exists(customOldIDSPath))
            {
                Debug.LogError("Could not find old PREspective IDs");
                return;
            }

            MappingView mappingView = new MappingView();

            string oldVersion = PlayerPrefs.GetString(PrepackageConstants.PREPACKAGE_PACKAGE_VERSION_OLD);
            string newVersion = PlayerPrefs.GetString(PrepackageConstants.PREPACKAGE_PACKAGE_VERSION_NEW);
            if (oldVersion == newVersion)
            {
                if (!EditorUtility.DisplayDialog("Version are the same, not importing!",
                    "The versions of PREspective are the same, the migration tool was halted. Do you want to migrate anyway?\r\nOriginal: v" +
                    oldVersion + "\r\nNew: v" + newVersion,
                    "Migrate anyway", "Cancel the migration"))
                {
                    return;
                }
            }

            if (mappingView.IsOldVersionHigher(oldVersion, newVersion))
            {
                EditorUtility.DisplayDialog("Cannot downgrade project",
                    "The new version of PREspective is older then the new version. Cannot migrate to older versions. The package was imported but the migration was not run.",
                    "Ok");
                return;
            }

            Dictionary<string, List<ScriptMapping>> versionsFromApi = GetVersionMappingsFromAPI(oldVersion, newVersion);
            List<ScriptMapping> scriptMappings = mappingView.CombineMappings(oldVersion, newVersion, versionsFromApi);

            Administration.Instance.ScriptMappingsOverride = scriptMappings;
            Administration.Instance.oldIDsOverride =
                JsonConvert.DeserializeObject<List<ClassModel>>(File.ReadAllText(customOldIDSPath));
            string moveTo = projectPath + constants.RelativeExportPath.Replace(".json", "_old.json");
            if (File.Exists(moveTo))
            {
                File.Move(moveTo, ProjectPathUtility.AddTimestamp(moveTo));
            }

            File.Move(customOldIDSPath, moveTo);

            IDView.ExportCurrentClassData(projectPath);
            ThreadUtil.RunMainThread(() =>
            {
                Administration.Instance.OverWriteMode = true;
                Administration.Instance.ShowInfoPopups = false;
                Administration.Instance.MigrateScenePrefabDependencies = false;

                Action onCompleteScenes = () =>
                {
                    ThreadUtil.RunMainThread(() =>
                    {
                        EditorUtility.DisplayDialog("Migration completed",
                            "Completed the migration, everything should be migrated to the new version. \r\nPlease check your project for any errors.",
                            "Ok");
                        Administration.Instance.OverWriteMode = false;
                        Administration.Instance.ShowInfoPopups = true;
                        Administration.Instance.MigrateScenePrefabDependencies = true;
                    });
                };
                Action onCompletePrefabs = () =>
                {
                    ThreadUtil.RunMainThread(() => { sceneView.MigrateAllScenes(projectPath, onCompleteScenes); });
                };
                ThreadUtil.RunThread(() =>
                {
                    prefabView.MigrateAllPrefabs(projectPath, projectPath,
                        onCompletePrefabs, scriptMappings);
                });
            });
        }

        /// <summary>
        /// Get all mappings from the API.
        /// </summary>
        /// <param name="oldVersion"></param>
        /// <param name="newVersion"></param>
        /// <returns></returns>
        private static Dictionary<string, List<ScriptMapping>> GetVersionMappingsFromAPI(string oldVersion,
            string newVersion)
        {
            MappingController mappingController = new MappingController();

            Dictionary<string, List<ScriptMapping>> allVersions = new Dictionary<string, List<ScriptMapping>>();
            WebClient wc = new WebClient();
            using (MemoryStream stream =
                new MemoryStream(wc.DownloadData(PrepackageConstants.PREPACKAGE_DOMAIN +
                                                 PrepackageConstants.PREPACKAGE_VERSIONS_PATH)))
            {
                string request = Encoding.ASCII.GetString(stream.ToArray());
                Debug.LogError(request);
                JArray data = JArray.Parse(request);


                foreach (JObject element in data)
                {
                    string version = (string) element["version"];
                    if (!mappingController.IsInsideVersions(version, oldVersion, newVersion)) continue;

                    string mappingUrl = (string) element["mappingUrl"];
                    if (string.IsNullOrEmpty(mappingUrl)) continue;

                    using (MemoryStream linkStream = new MemoryStream(wc.DownloadData(mappingUrl)))
                    {
                        Debug.Log("Downloaded mapping Version: " + version);
                        string mappingString = Encoding.ASCII.GetString(linkStream.ToArray());

                        List<ScriptMapping> mapping =
                            JsonConvert.DeserializeObject<List<ScriptMapping>>(mappingString);
                        allVersions[version] = mapping;
                    }
                }
            }

            return allVersions;
        }

        /// <summary>
        /// Started before the package is imported
        /// </summary>
        /// <param name="projectPath"></param>
        /// <param name="packageLocation"></param>
        private static void packageImportStarted(string projectPath, string packageLocation)
        {
            Debug.LogWarning("[PREpackage] Package import Started");
            IDView.ExportCurrentClassData(projectPath);
        }

        /// <summary>
        /// Gets the PREspective DDL versions
        /// </summary>
        /// <returns></returns>
        private static string GetPREspectiveDLLVersion()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name.ToLower().Contains("prespective"))
                {
                    return assembly.GetName().Version.ToString();
                }
            }

            return null;
        }
    }
}