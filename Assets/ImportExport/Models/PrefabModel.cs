
using System.IO;

namespace importerexporter.models
{
    public class PrefabModel
    {
        /// <summary>
        /// Class namespace and name
        /// </summary> 
        public string Path;

        /// <summary>
        /// Name of the file
        /// </summary>
        public string Name {
            get { return System.IO.Path.GetFileName(Path); }
        }

        /// <summary>
        /// The guid of the prefab
        /// </summary>
        public string Guid;

        public PrefabModel(string path, string guid)
        {
            this.Path = path;
            this.Guid = guid;
        }
    }
}
