#if UNITY_EDITOR
namespace u040.prespective.migrationtool
{
    public static class PrepackageConstants
    {
        public const string EXTENSION = "prepackage";
        public const string PREPACKAGE_PACKAGE_LOCATION = "MIGRATION_TOOL_UPDATER.PACKAGE_IMPORTING";
        public const string PREPACKAGE_PACKAGE_CONTENT = "MIGRATION_TOOL_UPDATER.PACKAGE_CONTENT";
        public const string PREPACKAGE_PACKAGE_VERSION_OLD = "MIGRATION_TOOL_UPDATER.PACKAGE_VERSION_OLD";
        public const string PREPACKAGE_PACKAGE_VERSION_NEW = "MIGRATION_TOOL_UPDATER.PACKAGE_VERSION_NEW";
        public const string PREPACKAGE_UPDATER = "MIGRATION_TOOL.UPDATER";
        
        public const string PREPACKAGE_DOMAIN = "https://versions.prespective-software.com/api";
        public const string PREPACKAGE_VERSIONS_PATH = "/versions";
    }
}
#endif