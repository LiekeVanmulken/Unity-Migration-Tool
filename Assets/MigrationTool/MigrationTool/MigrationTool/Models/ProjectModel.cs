#if UNITY_EDITOR || UNITY_EDITOR_BETA
using System.Collections.Generic;
using migrationtool.models;

/// <summary>
/// Project data
/// </summary>
public class ProjectModel
{
    public List<ClassModel> oldIDs;
    public List<ClassModel> newIDs;
    public List<ScriptMapping> scriptMappings;

    public ProjectModel(List<ClassModel> oldIDs, List<ClassModel> newIDs, List<ScriptMapping> scriptMappings = null)
    {
        this.oldIDs = oldIDs;
        this.newIDs = newIDs;
        this.scriptMappings = scriptMappings;
    }
}
#endif