using UnityEditor;
using UnityEngine;

namespace u040.prespective.migrationtoool
{
    class AfterImportScript : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (!PlayerPrefs.HasKey(PrepackageConstants.PREPACKAGE_PACKAGE_LOCATION))
            {
                return;
            }

            if (!PlayerPrefs.HasKey(PrepackageConstants.PREPACKAGE_PACKAGE_CONTENT))
            {
                return;
            }

            string[] packageFiles = PlayerPrefs.GetString(PrepackageConstants.PREPACKAGE_PACKAGE_CONTENT).Split(',');

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


            string packageLocation = PlayerPrefs.GetString(PrepackageConstants.PREPACKAGE_PACKAGE_LOCATION);


            PlayerPrefs.DeleteKey(PrepackageConstants.PREPACKAGE_PACKAGE_LOCATION);
            PlayerPrefs.DeleteKey(PrepackageConstants.PREPACKAGE_PACKAGE_CONTENT);


            PREpackageImporter.packageImportFinished(Application.dataPath, packageLocation);
        }
    }
}