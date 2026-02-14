using UnityEngine;
using System.Collections.Generic;

public class PoltiSituationInstance
{
    public PoltiSituation Template;

    public List<PoltiRoleInstance> Roles = new();

    public int Id => Template.Id;
    public string Name => Template.Name;
}

public class PoltiRoleInstance
{
    public PoltiRole Template;

    public bool IsAssigned;
    public CharacterConstraints AssignedCharacter;

    public List<PoltiRelationInstance> Relations = new();

    public PoltiRoleInstance(PoltiRole template)
    {
        Template = template;

        // Instantiate relation instances from template
        foreach (var rel in template.Relations)
        {
            Relations.Add(new PoltiRelationInstance(rel, this));
        }
    }

    public bool IsDead => Template.IsDead;
    public string Name => Template.Name;
}

public class PoltiRelationInstance
{
    public PoltiRelation Template;

    public PoltiRoleInstance Source;
    public PoltiRoleInstance Target; // Assigned later

    public PoltiRelationInstance(PoltiRelation template, PoltiRoleInstance source)
    {
        Template = template;
        Source = source;
    }

    public bool IsCompatible(List<PoltiRelationInstance> existingRelations)
    {
        // Delegate to template logic

        // Convert instances back to template relations for compatibility check
        var templateList = new List<PoltiRelation>();
        foreach (var r in existingRelations)
        {
            templateList.Add(r.Template);
        }

        return Template.IsCompatible(templateList);
    }
}