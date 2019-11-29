using u040.prespective.migrationtoool;
#if UNITY_EDITOR
using System.Linq;
using System;
using System.Reflection;
using System.Threading;
using migrationtool.windows;
using UnityEditor;
using UnityEngine;

namespace migrationtool.utility
{
    public class ThreadTestWindow : EditorWindow
    {
        // Add menu named "My Window" to the Window menu
        [MenuItem("Window/ThreadTestWindow2")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            ThreadTestWindow window = (ThreadTestWindow) EditorWindow.GetWindow(typeof(ThreadTestWindow));
            window.Show();
        }

        private bool isRunning;

        private long FileID_of_nested_PrefabInstance;
        private long FileID_of_object_in_nested_Prefab;

        void OnGUI()
        {
//            if (GUILayout.Button("Get PREspective version"))
//            {
//                GetDLLVersion();
//            }
            if (GUILayout.Button("Map mappings"))
            {
                new MappingView().GenerateNewMapping();
            }

            if (GUILayout.Button("test zip"))
            {
                string package = EditorUtility.OpenFilePanel("open packages",
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "prepackage");
                if (string.IsNullOrEmpty(package))
                {
                    return;
                }

                Unzipper.ParseUnityPackagesToFiles(package);
            }


            FileID_of_nested_PrefabInstance =
                EditorGUILayout.LongField("FileID_of_nested_PrefabInstance", FileID_of_nested_PrefabInstance);
            FileID_of_object_in_nested_Prefab = EditorGUILayout.LongField("FileID_of_object_in_nested_Prefab",
                FileID_of_object_in_nested_Prefab);

            long result = (FileID_of_nested_PrefabInstance ^ FileID_of_object_in_nested_Prefab) & 0x7fffffffffffffff;
            EditorGUILayout.LongField(result);

            GUILayout.Space(50);

            if (GUILayout.Button("AssetDatabaseTest"))
            {
                Assembly executingAssembly = Assembly.GetExecutingAssembly();
                Type[] types = executingAssembly.GetTypes();


                string[] typeStringArray =
                    types.Select(type => type.FullName + " : " + type.Namespace + "." + type.Name).ToArray();
                string joinedTypeString = string.Join("\n", typeStringArray);
                Debug.Log(joinedTypeString);


//                string dllGuid = null;
//                string[] assets = AssetDatabase.FindAssets("");
//                foreach (string assetGuid in assets)
//                {
//                    string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
//                    Object[] assemblyObjects =
//                        AssetDatabase.LoadAllAssetsAtPath(assetPath);
//                    for (int i = 0; i < assemblyObjects.Length; i++)
//                    {
//                        long dllFileId;
//
//                        if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(assemblyObjects[i], out dllGuid, out dllFileId))
//                        {
//                            Debug.Log(assemblyObjects[i].name + " " + dllFileId.ToString() + " " + dllGuid );
//                        }
//                    }   
//                }
            }

            GUILayout.Space(20);
            if (GUILayout.Button("Start Thread"))
            {
                isRunning = true;
                var thread = new Thread(() =>
                {
                    try
                    {
                        test(() => OnComplete());
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                });
                thread.Start();
            }

            GUILayout.Label(isRunning ? "Running" : "Stopped");
        }

        private void OnComplete()
        {
            isRunning = false;
        }

        

        private void test(Action OnComplete)
        {
            string result = null;
            ThreadUtil.RunMainThread(() =>
            {
                OptionsWizard optionsWizard =
                    OptionsWizard.CreateWizard("Class cannot be found, select which one to choose",
                        "a", new string[] {"aa", "b", "c"},
                        resultCompleted => { Debug.Log("Select called, result: " + resultCompleted); },
                        () => { Debug.Log("Ignore called"); });
            });

            while (string.IsNullOrEmpty(result))
            {
                Debug.Log("result : " + result);
                Thread.Sleep(1000);
            }

            Debug.Log("result : " + result);
            Debug.Log("Completed");
            OnComplete();
        }
    }
}
#endif