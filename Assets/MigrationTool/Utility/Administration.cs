using System.Collections.Generic;
using migrationtool.models;

namespace migrationtool.utility
{
    public class Administration : Singleton<Administration>
    {
        //todo : change this, you need to know this, this is bad design
        public List<ClassModel> oldIDsOverride;
        public List<ClassModel> newIDsOverride;
        public List<ScriptMapping> ScriptMappingsOverride;
        
        
        /// <summary>
        /// Sets whether the migrationTool will overwrite scenes and prefabs.
        /// false -> make a copy with a timestamp
        /// true -> overwrite the scene file
        /// </summary>
        public bool OverWriteMode = false;

        /// <summary>
        /// Setting that turns off popups so it can be migrated without user intervention
        /// // todo : should this be ported to the constants?
        /// </summary>
        public bool ShowInfoPopups = false;

        /// <summary>
        /// Automatically migrate any prefab that is in a scene
        /// </summary>
        public bool MigrateScenePrefabDependencies = true;
    }
}