# Scene Import Export Tool

This is a tool to import scenes between projects.
This will change the GUID and fileID in the the scene and copy it to a new file.
Which results in fixing the script references in the scenefile including all references.

## How to use

- Have two projects, the project where you want to copy from (project A) and the project you want to copy to (project B).
- Copy the scene file from project A to project B.
- Open Project B.
- Open from the menu Window/Scene import window.
- Click the "Set old project path" and a popup window will appear. Select the Assets folder of project A.
- Click Import and a popup window will appear.
- Select the scene (in project B) you copied earlier to project B.
- The scene is now being imported.
- If there are double classes, there will be a popup of which you want to use.
- Then there will be a popup that says the process has completed and has been written to a new file. (This file has the same path as the original scene + the current time)

You should now be able to open that scenen with all references intact.

