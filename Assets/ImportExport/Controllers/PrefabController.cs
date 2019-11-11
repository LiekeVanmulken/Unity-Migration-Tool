#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using importerexporter.models;
using importerexporter.utility;

namespace importerexporter.controllers
{
    public class PrefabController
    {
        private readonly Constants constants = Constants.Instance;

        /// <summary>
        /// Get all prefabs in the projects and parses them to prefab models
        /// </summary>
        /// <param name="path">AssetPath of the project</param>
        /// <returns></returns>
        public List<PrefabModel> ExportPrefabs(string path)
        {
            //Get all prefabs
            string[] prefabMetaFiles = Directory.GetFiles(path, "*.prefab.meta", SearchOption.AllDirectories);

            //Parse all guids
            List<PrefabModel> prefabModels = new List<PrefabModel>(prefabMetaFiles.Length);
            for (var i = 0; i < prefabMetaFiles.Length; i++)
            {
                prefabModels.Add(ParsePrefabFile(prefabMetaFiles[i]));
            }

            return prefabModels;
        }

        /// <summary>
        /// Generates a PrefabModel from a .prefab.meta file
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        private PrefabModel ParsePrefabFile(string file)
        {
            IEnumerable<string> lines = File.ReadLines(file);
            foreach (string line in lines)
            {
                Match match = constants.RegexGuid.Match(line);
                if (!match.Success) continue;

                return new PrefabModel(file, match.Value);
            }

            throw new NullReferenceException("Could not find GUID in prefab meta file : " + file);
        }
    }
}
#endif