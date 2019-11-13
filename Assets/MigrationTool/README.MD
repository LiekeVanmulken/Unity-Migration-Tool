# Unity-Scene-Exporter

The tool enables you to export scenes from one project to another without breaking script references.
Migrate data to new field- and new class names and directly migrate prefabs.

The migration tool started off as a hobby project that quickly became a work project. So i'd also like to thank [Unit040](https://www.unit040.com) for keeping this open source.

## The Problem

1. When moving a scene file from one project to another, unity will lose all idea of what scripts are attached to what gameobjects. This is because it uses a filedID and GUID to reference scripts which are project specific. To fix these references the tool will search the old project for the old GUIDs and generate the corresponding fileID and map these to the class name between projects. Causing unity to find the reference to the script and solving all script references in the scene.

The most helpful information about the script reference issue can be found on the unity forum [here](https://forum.unity.com/threads/yaml-fileid-hash-function-for-dll-scripts.252075/).


2. We needed a way to migrate between field names and class names as we found that these often changed between versions. Any name change, whether it's in the Namespace or capitalization changes, can now be ported with this tool.

## Main usecases

1. This is currently being used to port Source code demo projects to DLL projects.
2. We also use the tool to migrate data between versions of our dll without losing scene data.

To enable the migration between versions it can map field- and classnames.
For example the following script can be migrated with all data included to the new version of the script.

Original script:

```
using System;
using UnityEngine;

[Serializable]
public class TestScript_test : MonoBehaviour
{
    public string test;
    
    [SerializeField]
    private TestScriptSubClass testScriptSubClass_ = new TestScriptSubClass("a", "b");
    
    [Serializable]
    public class TestScriptSubClass
    {
        [SerializeField] private string privateString_;
        public string publicString_;

        public TestScriptSubClass(string privateString, string publicString)
        {
            this.privateString_ = privateString;
            this.publicString_ = publicString;
        }
    }
}
```

There are three fields that will be migrated.

![Original Values](https://raw.githubusercontent.com/WouterVanmulken/Unity-Scene-Exporter/master/Images/originalValues.png)

Migrated script:

```
using System;
using UnityEngine;

[Serializable]
public class TestScript1 : MonoBehaviour
{
    public string test2;
    
    [SerializeField]
    private TestScriptSubClass testScriptSubClass = new TestScriptSubClass("a", "b");

    public class testingSubClass
    {
        [SerializeField]
        private string test2;

        public string publicTest;

    }
    [Serializable]
    public class TestScriptSubClass
    {
        [SerializeField] private string privateString;
        public string publicString;

        public TestScriptSubClass(string privateString, string publicString)
        {
            this.privateString = privateString;
            this.publicString = publicString;
        }
    }
}
```

All values are migrated to the new values in the new project.

![Migrated Values](https://raw.githubusercontent.com/WouterVanmulken/Unity-Scene-Exporter/master/Images/migratedValues.png)


## How to use

### Installation

1. Have two projects, the old project and the new project. (The old project will have the scene we would like to export). One way to accomplish this would be to literally copy the project in the file explorer, this way you immediately have access to all your resources. 
2. Download the latest unity package from the [github releases page](https://github.com/WouterVanmulken/Unity-Scene-Exporter/releases/).
3. Import the [package](https://github.com/WouterVanmulken/Unity-Scene-Exporter/releases/) in both projects or copy the `MigrationTool` folder from the source to both projects.

Note: The project includes both JsonDotNet and YamlDotNet. If you already use these libraries there might be errors that it has duplicate files. Just delete one of the versions.

### Usage

| WARNING: ALWAYS BACK UP YOUR PROJECT. Although this tool is non-destructive, we're working with the source files of the project so make sure you ALWAYS have a backup. |
| --- |

1. In the old project open `Window/Migration Tool` on the menubar and open the window.
2. Click the `Export Class Data of the current project` button and the project will start to export the IDs (these will be saved to `<project_path>/MigrationTool/Export/Export.json`).
3. Now do the same for the new project.
4. In the new project open `Window/Migration Tool` on the menubar and open the window.
5. Click the `Export Class Data of the current project` button and the project will start to export the IDs (these will be saved to `<project_path>/MigrationTool/Export/Export.json`).
6. When the IDs have been exported in the old and new project, go to the new project and open the `Window/Migration Tool` window again.
7. Click the `Migrate Scene to current project` button. The tool will open a window to select which scene to use.
8. Select the scenefile to migrate.
9. The tool will start converting the scene.
10. The tool will show popups to map classes for which it can't find the new class. Please select the right class, it has a filter field to make it easier.
11. After it can map all classes it wil check whether or not data needs to be migrated. If it doesn't it's exported the scene. But if the names of the fields on the scripts have been changed the mergewindow will appear. You can now select which old field should be mapped to which new field.
12. When you're done mapping the fields (make sure to do this as best as possible as mismatched fields will result in data loss) press the `Merge` button.

Note: If you have prefabs in the scene, this might popup multiple times as prefabs get proccessed separately (nested prefabs fieldMappings are currently not yet supported).
14. The tool will now  change all fields of the scripts to the new fields.

The Tool will migrate the scene and write it to the Assets folder of the project. All referenced prefabs will be migrated to the Assets folder as well.
