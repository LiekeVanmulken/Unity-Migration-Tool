using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using migrationtool.controllers;
using migrationtool.models;
using migrationtool.utility;
using migrationtool.windows;
using Newtonsoft.Json;
using UnityEngine;

public class MappingController
{
    IDController idController = new IDController();

    /// <summary>
    /// Generate a mapping of all foundScripts in a project.
    /// Which means it creates a mapping between versions.
    /// </summary>
    /// <param name="oldIDs"></param>
    /// <param name="newIDs"></param>
    /// <returns></returns>
    public void MapAllClasses(List<ClassModel> oldIDs, List<ClassModel> newIDs, Action<List<FoundScript>> onComplete)
    {
        if (oldIDs == null || newIDs == null)
        {
            Debug.LogError("Old or new IDS are null. Cannot map without the old and new IDs");
        }

        List<FoundScript> unmergedFoundScripts = MappAllClasses(oldIDs, newIDs);
        List<FoundScript> unmappedFoundScripts = unmergedFoundScripts
            .Where(script => script.HasBeenMapped == FoundScript.MappedState.NotMapped).ToList();
        if (unmappedFoundScripts.Count == 0)
        {
            onComplete(unmappedFoundScripts);
            return;
        }

        ThreadUtil.RunMainThread(() =>
        {
            MergeWizard mergeWizard = MergeWizard.CreateWizard(unmappedFoundScripts);
            mergeWizard.onComplete = (mergedFoundScripts) =>
            {
                List<FoundScript> completed = unmappedFoundScripts.Merge(mergedFoundScripts);
                onComplete(completed);
            };
        });
    }

    /// <summary>
    /// Maps all classes to foundScripts in a project
    /// </summary>
    /// <param name="oldIDs"></param>
    /// <param name="newIDs"></param>
    /// <returns></returns>
    private List<FoundScript> MappAllClasses(List<ClassModel> oldIDs, List<ClassModel> newIDs)
    {
        List<FoundScript> foundScripts = new List<FoundScript>();
        List<ClassModel> classesToMap = new List<ClassModel>();
        foreach (ClassModel oldID in oldIDs)
        {
            var result = idController.FindNewID(newIDs, oldID, false);
            if (result == null)
            {
                classesToMap.Add(oldID);
            }

            idController.FindMappingRecursively(newIDs, ref foundScripts, oldID);
        }

        GC.Collect();

        Debug.Log(string.Join("\r\n", classesToMap.Select(model => model.FullName).ToArray()));
        Debug.Log("Mapped classes. Total: " + oldIDs.Count + " classesThatNeedToBeMapped: " + classesToMap.Count);
        

        return foundScripts;
    }

    public static List<FoundScript> DeserializeMapping(string path)
    {
        return JsonConvert.DeserializeObject<List<FoundScript>>(File.ReadAllText(path));
    }

//    public List<FoundScript> FixMapping(List<FoundScript> foundScripts, List<ClassModel> oldIDs,
//        List<ClassModel> newIDs)
//    {
//        for (var i = 0; i < foundScripts.Count; i++)
//        {
//            ClassModel old = foundScripts[i].oldClassModel;
//            ClassModel oldReplacement = findClassModelRecursive(oldIDs, old);
//            if (oldReplacement == null)
//            {
//                Debug.LogError("Could not find match for class in old IDs: " + old.FullName);
//            }
//            else
//            {
//                foundScripts[i].oldClassModel = oldReplacement;
//            }
//
//
//            ClassModel newClass = foundScripts[i].newClassModel;
//            ClassModel newReplacement = findClassModelRecursive(newIDs, newClass);
//            if (newReplacement == null)
//            {
//                Debug.LogError("Could not find match for class in new IDs: " + newClass.FullName);
//            }
//            else
//            {
//                foundScripts[i].newClassModel = newReplacement;
//            }
//        }
//
//        return foundScripts;
//    }

    /// <summary>
    /// Find the classModel in a list recursively 
    /// </summary>
    /// <param name="IDs"></param>
    /// <param name="toFind"></param>
    /// <returns></returns>
    public static ClassModel FindClassModelRecursive(List<ClassModel> IDs, ClassModel toFind)
    {
        string toFindFullName = toFind.FullName;
        // First loop through all of them to check the first layer. 
        // If we didn't use a separate loop we would get a subObject over a root script
        // (root scripts are the ones with the guids and fileIDs) 
        foreach (ClassModel id in IDs)
        {
            if (id.FullName == toFindFullName)
            {
                return id;
            }
        }
        //Check if their fields might have the type
        foreach (ClassModel id in IDs)
        {
            if (id.Fields == null || id.Fields.Length == 0)
            {
                continue;
            }
            
            ClassModel match = FindClassModelRecursive(id.Fields.Select(model => model.Type).ToList(), toFind);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }
}