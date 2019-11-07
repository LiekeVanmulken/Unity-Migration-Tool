
using System.Collections.Generic;
using importerexporter.models;

/// <summary>
/// Project data
/// </summary>
public class ProjectModel
{
    public List<ClassModel> classes;
    public List<PrefabModel> prefabs;
    public List<FoundScript> foundScripts;

    public ProjectModel(List<ClassModel> classes, List<PrefabModel> prefabs, List<FoundScript> foundScripts = null)
    {
        this.classes = classes;
        this.prefabs = prefabs;
        this.foundScripts = foundScripts;
    }
}
