#if UNITY_EDITOR || UNITY_EDITOR_BETA
using System;

namespace migrationtool.models
{
    public class PrefabModel
    {
        /// <summary>
        /// Class namespace and name
        /// </summary> 
        public string Path;
        public string MetaPath;

        /// <summary>
        /// The guid of the prefab
        /// </summary>
        public string Guid;

        public PrefabModel(string path, string guid)
        {
            if (path.EndsWith(".meta"))
            {
                Path = path.Replace(".meta", "");
                MetaPath = path;
            }
            else if(path.EndsWith(".prefab"))
            {
                Path = path;
                MetaPath = path + ".meta";
            }
            else
            {
                throw new FormatException("Cannot parse extension of prefab");
            }
            this.Guid = guid;
        }
    }
}
#endif