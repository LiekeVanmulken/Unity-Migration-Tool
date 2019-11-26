using UnityEditor;
using UnityEngine;

namespace u040.prespective.migrationtoool
{
    class AfterImportScript : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (!PlayerPrefs.HasKey(PREpackageImporter.PREPACKAGE_PACKAGE_LOCATION))
            {
                return;
            }

            if (!PlayerPrefs.HasKey(PREpackageImporter.PREPACKAGE_PACKAGE_CONTENT))
            {
                return;
            }

            string[] packageFiles = PlayerPrefs.GetString(PREpackageImporter.PREPACKAGE_PACKAGE_CONTENT).Split(',');

            bool isPackageImport = false;
            foreach (string importedAsset in importedAssets)
            {
                foreach (string packageFile in packageFiles)
                {
                    if (importedAsset.EndsWith(packageFile))
                    {
                        isPackageImport = true;
                        break;
                    }
                }

                if (isPackageImport)
                {
                    break;
                }
            }

            if (!isPackageImport)
            {
                return;
            }


            string packageLocation = PlayerPrefs.GetString(PREpackageImporter.PREPACKAGE_PACKAGE_LOCATION);


            PlayerPrefs.DeleteKey(PREpackageImporter.PREPACKAGE_PACKAGE_LOCATION);
            PlayerPrefs.DeleteKey(PREpackageImporter.PREPACKAGE_PACKAGE_CONTENT);


            PREpackageImporter.packageImportFinished(Application.dataPath, packageLocation);
        }
    }
}