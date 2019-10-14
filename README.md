# Unity-Scene-Exporter
Exporting scenes from one project to another without breaking script references.

When moving a scene file from one project to another, unity will lose all idea of what scripts are attached to what gameobjects. This is because it uses a filedID and GUID to reference scripts which are project specific. To fix these references the tool will search the old project for the old GUIDs and generate the corresponding fileID. Causing unity to find the reference to the script and solving all script references in the scene.

This is currently mainly used to port Source code demo projects to DLL projects. It started of as a hobby project that quickly became a work project. So i'd also like to thank [unit040](https://www.unit040.com) for keeping this open source.

The most helpful information about this issue can be found on the unity forum [here](https://forum.unity.com/threads/yaml-fileid-hash-function-for-dll-scripts.252075/).

## How to use

### Installation:
1. Have two projects, the old project and the new project. (The old project will have the scene we'll export)
2. Copy the `Assets/ImportExport/` folder to both projects.
3. Import [json.net](https://assetstore.unity.com/packages/tools/input-management/json-net-for-unity-11347) from the unity assetstore
4. Import [YamlDotNet](https://assetstore.unity.com/packages/tools/integration/yamldotnet-for-unity-36292) from the unity assetstore

### Usage

1. In the old project open `Window/Scene import window` on the menubar and open the window.
2. Click the `Export IDs` button and the project will start to export the IDs (these will be saved to `<project_path>/ImportExport/Export/Export.json`).
3. When the IDs have been exported in the old project, go to the new project and open the `Window/Scene import window` window.
4. Click the `Import IDs` button, the tool will then open a window. This window selects which  export to use. Go to your old project and select the `<project_path>/ImportExport/Export/Export.json` file.
5. The tool will then open a window to select which scene to use.
6. Select the scenefile you wish to update.

The Tool will make a copy of the file and write it to the same folder as the original folder.
In case the fields on the class cannot be matched to the fields on the new class, a merge window will appear where you can select what to do with these fields.




