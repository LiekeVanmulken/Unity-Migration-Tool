
using System.Collections.Generic;
using importerexporter.models;

/// <summary>
/// Project data
/// </summary>
public class ProjectModel
{
    public List<ClassModel> oldIDs;
    public List<ClassModel> newIDs;
    public List<FoundScript> foundScripts;

    public ProjectModel(List<ClassModel> oldIDs, List<ClassModel> newIDs, List<FoundScript> foundScripts = null)
    {
        this.oldIDs = oldIDs;
        this.newIDs = newIDs;
        this.foundScripts = foundScripts;
    }
}
