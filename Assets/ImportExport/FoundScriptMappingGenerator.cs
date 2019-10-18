using System;
using System.Collections.Generic;
using System.Linq;
using importerexporter;
using importerexporter.models;
using UnityEngine;

public class FoundScriptMappingGenerator
{
    IDUtility idUtility = IDUtility.Instance;
    #region Singleton

    private static FoundScriptMappingGenerator instance = null;

    private static readonly object padlock = new object();

    FoundScriptMappingGenerator()
    {
    }

    public static FoundScriptMappingGenerator Instance
    {
        get
        {
            lock (padlock)
            {
                if (instance == null)
                {
                    instance = new FoundScriptMappingGenerator();
                }

                return instance;
            }
        }
    }

    #endregion


    public void GenerateMapping(List<ClassData> oldIDs, List<ClassData> newIDs,
        ref List<FoundScript> foundScripts)
    {
        foreach (ClassData oldID in oldIDs)
        {
            GenerateMappingRecursive(oldIDs, newIDs, ref foundScripts, oldID);
        }
    }

    private void GenerateMappingRecursive(List<ClassData> oldIDs, List<ClassData> newIDs,
        ref List<FoundScript> foundScripts,
        ClassData oldID)
    {
        if (oldID?.Fields == null || oldID.Fields.Length == 0)
        {
            return;
        }

        FoundScript existingFoundScript = foundScripts.FirstOrDefault(script =>
            script.OldClassData.Guid == oldID.Guid && script.OldClassData.FileID == oldID.FileID);

        if (existingFoundScript != null)
        {
            Debug.LogWarning("Double the ID, this really shouldn't happen");
            throw new NotImplementedException("Double the id in the mapping");
            return;
        }
        
        string oldGuid = oldID.Guid;
        string oldFileID = oldID.FileID;
            
        ClassData oldClassData = oldIDs.FirstOrDefault(                            //todo : check if this works here                
            currentOldFileData => currentOldFileData.Guid.Equals(oldGuid) &&
                                  currentOldFileData.FileID.Equals("11500000")
                                  ||
                                  currentOldFileData.Guid.Equals(oldGuid) &&
                                  currentOldFileData.FileID.Equals(oldFileID)
        );
        
        ClassData newClassData = idUtility.findNewID(newIDs, oldClassData);
        if (newClassData == null)
        {
            Debug.LogError("Could not make mapping for class : " + oldID.Name);
            return;
        }

        existingFoundScript = new FoundScript()
        {
            OldClassData = oldID,
            NewClassData = newClassData
        };
        existingFoundScript.CheckHasBeenMapped();
        foundScripts.Add(existingFoundScript);
        
        if (oldID.Fields?.Length == 0) return;
        
        foreach (FieldData oldIdField in oldID.Fields)
        {
            GenerateMappingRecursive(oldIDs, newIDs, ref foundScripts, oldIdField.Type);
        }
    }
}