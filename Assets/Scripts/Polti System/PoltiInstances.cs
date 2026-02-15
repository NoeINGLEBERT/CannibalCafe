using System.Collections.Generic;
using System;

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

    public event Action<CharacterConstraints> OnRoleAssigned;

    public PoltiRoleInstance(PoltiRole template)
    {
        Template = template;
    }

    public void AssignCharacter(CharacterConstraints character)
    {
        AssignedCharacter = character;
        IsAssigned = true;

        OnRoleAssigned?.Invoke(character);
    }

    public bool IsDead => Template.IsDead;
    public string Name => Template.Name;
}

public class PoltiRelationInstance
{
    public PoltiRelation Template;

    public PoltiRoleInstance RoleTarget;
    public CharacterConstraints CharacterTarget;

    public PoltiRelationInstance(PoltiRelation template, PoltiRoleInstance roleTarget)
    {
        Template = template;
        RoleTarget = roleTarget;

        RoleTarget.OnRoleAssigned += OnTargetRoleAssigned;
    }

    private void OnTargetRoleAssigned(CharacterConstraints character)
    {
        CharacterTarget = character;
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