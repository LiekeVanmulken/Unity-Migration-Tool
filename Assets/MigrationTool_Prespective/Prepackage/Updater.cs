#if UNITY_EDITOR || UNITY_EDITOR_BETA
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace u040.prespective.migrationtool
{
    [InitializeOnLoad]
    [ObfuscationAttribute(Exclude = true, ApplyToMembers = true)]
    public class Updater : EditorWindow
    {
        private string version;
        private string latestVersion;

        private bool madeABackup = false;

        private const string DEFAULT = "[Assembly not loaded]";
        
        static Updater()
        {
            EditorApplication.update += EditorUpdate;
        }
        
        [MenuItem("PREspective/Updater")]
        static void Init()
        {
            GetWindow<Updater>("Updater").Show();
        }

        private static void EditorUpdate()
        {
            EditorApplication.update -= EditorUpdate;
            
            string version = CacheVersion();
            
            if (version == DEFAULT)
            {
                // Is source
                return;
            }

            if (PlayerPrefs.HasKey(PrepackageConstants.PREPACKAGE_UPDATER))
            {
                int result = PlayerPrefs.GetInt(PrepackageConstants.PREPACKAGE_UPDATER);
                if (result == 0 || result == 1) // user already said no
                {
                    return;
                }
            }

            string latestVersion = CacheLatestVersion(); //todo : why is all this static, no pls
            if (version == latestVersion)
            {
                //Already on the latest version
                return;
            }

            if (version != latestVersion || version != DEFAULT || latestVersion != DEFAULT)
            {
                if (EditorUtility.DisplayDialog("PREspective version",
                    "A new version of PREspective is available!\r\n\r\nYou can update at any time by going to:\r\nPREspective/Updater",
                    "Update now", "Ignore"))
                {
                    GetWindow<Updater>("PREpackage Updater").Show();
                    PlayerPrefs.SetInt(PrepackageConstants.PREPACKAGE_UPDATER, 1); // user said no
                }
                else
                {
                    PlayerPrefs.SetInt(PrepackageConstants.PREPACKAGE_UPDATER, 0); // user said no
                }
            }
        }

        private void OnEnable()
        {
            version = CacheVersion(version);
            latestVersion = CacheLatestVersion(latestVersion);
        }

       
        void OnGUI()
        {
            var labelStyle = GUI.skin.label;
            labelStyle.wordWrap = true;

            GUILayout.Label("Updater", EditorStyles.boldLabel);
            GUILayout.Label("Version: " + version);
            GUILayout.Label("Latest Version: " + latestVersion);

            EditorGUILayout.Separator();

            if (version == latestVersion)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("You have the latest version of PREspective!", labelStyle);
                return;
            }

            if (latestVersion == DEFAULT)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Could not find latest PREspective version on the server. Try again later.",
                    labelStyle);
                return;
            }

            if (version == DEFAULT)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Could not find PREspective in the project.", labelStyle);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(
                "To upgrade the version of PREspective, scene and prefab files will be permanently altered. Please be sure to make a backup! ",
                labelStyle);
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            if (version == latestVersion)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("You have the latest version of PREspective!", labelStyle);
                return;
            }

            if (latestVersion == DEFAULT)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Could not find latest PREspective version on the server. Try again later.",
                    labelStyle);
                return;
            }

            if (version == DEFAULT)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Could not find PREspective in the project.", labelStyle);
                return;
            }

            madeABackup = GUILayout.Toggle(madeABackup, "I've made a backup.");
            EditorGUI.BeginDisabledGroup(!madeABackup);
            EditorGUILayout.Space();
            if (GUILayout.Button("Upgrade to the latest version"))
            {
                PREpackageImporter.MigratePREspective();
            }

            EditorGUI.EndDisabledGroup();
        }

        private static string CacheVersion(string version = null)
        {
            version = GetPREspectiveDLLVersion();
            if (version == null)
            {
                version = DEFAULT;
            }
            else
            {
                version = "v" + version;
            }

            return version;
        }

        private static string CacheLatestVersion(string latestVersion = null)
        {
            latestVersion = GetLatestVersion();
            if (latestVersion == null)
            {
                latestVersion = DEFAULT;
            }
            else
            {
                latestVersion = "v" + latestVersion;
            }

            return latestVersion;
        }

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

        private static string GetLatestVersion()
        {
            try
            {
                WebClient client = new WebClient();
                using (MemoryStream stream =
                    new MemoryStream(client.DownloadData(PrepackageConstants.PREPACKAGE_DOMAIN +
                                                         PrepackageConstants.PREPACKAGE_VERSIONS_PATH)))
                {
                    string request = Encoding.ASCII.GetString(stream.ToArray());
                    JArray packageVersions = JArray.Parse(request);

                    JObject versionToUse = (JObject) packageVersions[packageVersions.Count - 1];
                    return (string) versionToUse["version"];
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Could not reach the version api. \r\nError: " + e);
                return DEFAULT;
            }
        }
    }
}

#endif