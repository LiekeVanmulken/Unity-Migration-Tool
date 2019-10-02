# Unity-Scene-Exporter
Exporting scenes from one project to another without breaking script references.

When moving a scene file from one project to another, unity will lose all idea of what scripts are attached to what gameobjects. This is because it uses a filedID and GUID to reference scripts which are project specific. To fix these references the tool will search the old project for the old GUIDs and generate the corresponding fileID. Causing unity to find the reference to the script and solving all script references in the scene.

This is currently mainly used to port Source code demo projects to DLL projects. It started of as a hobby project that quickly became a work project. So i'd also like to thank [unit040](https://www.unit040.com) for keeping this open source.

The most helpful information about this issue can be found on the unity forum [here](https://forum.unity.com/threads/yaml-fileid-hash-function-for-dll-scripts.252075/).
