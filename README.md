# Unity-Scene-Exporter

The tool enables you to export scenes from one project to another without breaking script references.

## The Problem

When moving a scene file from one project to another, unity will lose all idea of what scripts are attached to what gameobjects. This is because it uses a filedID and GUID to reference scripts which are project specific. To fix these references the tool will search the old project for the old GUIDs and generate the corresponding fileID. Causing unity to find the reference to the script and solving all script references in the scene.

The most helpful information about this issue can be found on the unity forum [here](https://forum.unity.com/threads/yaml-fileid-hash-function-for-dll-scripts.252075/).

## Main usecase

This is currently used to port Source code demo projects to DLL projects. It started of as a hobby project that quickly became a work project. So i'd also like to thank [Unit040](https://www.unit040.com) for keeping this open source.

## How to use

### Installation

1. Have two projects, the old project and the new project. (The old project will have the scene we would like to export).One way to accomplish this would be to literally copy the project in the file explorer, this way you immediately have access to all your resources. 
2. Download the latest unity package from the [github releases page](https://github.com/WouterVanmulken/Unity-Scene-Exporter/releases/).
3. Import the package in both projects.

Note

### Usage

| WARNING: ALWAYS BACK UP YOUR PROJECT. Although this tool is non-destructive, we're working with the source files of the project so make sure you ALWAYS have a backup. |
| --- |

1. In the old project open `Window/Migration Tool` on the menubar and open the window.
2. Click the `Export Class Data of the current project` button and the project will start to export the IDs (these will be saved to `<project_path>/MigrationTool/Export/Export.json`).
3. Now do the same for the new project.
4. In the new project open `Window/Migration Tool` on the menubar and open the window.
5. Click the `Export Class Data of the current project` button and the project will start to export the IDs (these will be saved to `<project_path>/MigrationTool/Export/Export.json`).
6. When the IDs have been exported in the old and new project, go to the new project and open the `Window/Migration Tool` window.
7. Click the `Migrate Scene` button. The tool will open a window to select which scene to use.
8. Select the scenefile you wish to migrate.
9. The tool will now start converting the scene.
10. The tool will show popups to map classes for which it can't find the new class. Please select the right class, it has a filter field to make it easier.
11. After it can mapp all classes it wil check whether or not data needs to be migrated. If it doesn't it's exported the scene. But if the fields of the classes that have been changed the mergewindow will appear where you can select where to map the fields to.
12. When you're done mapping the fields (make sure to do this as best as possible as mismatched fields will result in dataloss) press the `Merge` button.

Note: If you have prefabs in the scene, this might popup multiple times as prefabs get proccessed separately (nested prefabs fieldMappings are currently not yet supported).
14. The tool will now  change all fields of the scripts to the new fields.

The Tool will migrate the scene and write it to the Assets folder of the project. All referenced prefabs will be migrated to the Assets folder as well.
