#if UNITY_EDITOR
using System.Collections.Generic;
using migrationtool.models;

namespace migrationtool.windows
{
    public class Administration : Singleton<Administration>
    {
        public List<ClassModel> oldIDsOverride;
        public bool OverwriteFiles;
        public bool showPopups;
    }
}
#endif