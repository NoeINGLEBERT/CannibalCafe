using System.Collections.Generic;
using System;
using UnityEngine;

public class PoltiSituationInstance
{
    public PoltiSituation Template;

    public List<PoltiRoleInstance> Roles = new();

    public int Id => Template.Id;
    public string Name => Template.Name;
}

public class PoltiRoleInstance
{
    public int Index;

    public PoltiRole Template;
    public PoltiSituationInstance Situation;

    public bool IsAssigned;
    public CharacterConstraints AssignedCharacter;

    public List<PoltiRelationInstance> outRelations = new();

    public List<PoltiRelationInstance> inRelations = new();

    public PoltiRoleInstance(int index, PoltiRole template, PoltiSituationInstance situation)
    {
        Index = index;
        Template = template;
        Situation = situation;
    }

    public AssignmentResult AssignCharacter(CharacterConstraints character)
    {
        int addedAssigned = 0;
        int addedUnassigned = 0;

        // Let relations validate first
        foreach (var relation in inRelations)
        {
            var relationResult = relation.OnTargetRoleAssigned(character);

            if (!relationResult.Success)
                return new AssignmentResult(false);

            addedAssigned += relationResult.DeltaAssignedRelation;
            addedUnassigned += relationResult.DeltaUnassignedRelation;
        }

        // If everything is valid, commit
        AssignedCharacter = character;
        IsAssigned = character != null;

        return new AssignmentResult(true, addedAssigned, addedUnassigned);
    }

    public bool IsDead => Template.IsDead;
    public string Name => Template.Name;
}

public class PoltiRelationInstance
{
    public PoltiRelation Template;

    public CharacterConstraints assignedCharacter;

    public PoltiRoleInstance RoleTarget;
    public CharacterConstraints CharacterTarget;

    public PoltiRelationInstance(PoltiRelation template, PoltiRoleInstance roleTarget)
    {
        Template = template;
        RoleTarget = roleTarget;

        RoleTarget.inRelations.Add(this);
    }

    public AssignmentResult OnTargetRoleAssigned(CharacterConstraints character)
    {
        if (CharacterTarget != null)
            return new AssignmentResult(CharacterTarget == character);

        CharacterTarget = character;

        if (assignedCharacter != null)
        {
            switch (IsCompatible(assignedCharacter.Relations))
            {
                case RelationCompatibility.Compatible:
                    return new AssignmentResult(true, 1, -1);

                case RelationCompatibility.Redundant:
                    //assignedCharacter.Relations.Remove(this);
                    return new AssignmentResult(true, 0, -1);

                case RelationCompatibility.Incompatible:
                    return new AssignmentResult(false);
            }
        }

        return new AssignmentResult(true);
    }

    public RelationCompatibility IsCompatible(List<PoltiRelationInstance> existingRelations)
    {
        if (CharacterTarget == null)
            return RelationCompatibility.Compatible;

        RelationCompatibility compatibility = RelationCompatibility.Compatible;

        foreach (var existing in existingRelations)
        {
            if (existing.CharacterTarget == null)
                continue;

            switch (Template.IsCompatible(existing.Template, CharacterTarget, existing.CharacterTarget))
            {
                case RelationCompatibility.Redundant:
                    if (this != existing)
                    {
                        Debug.LogWarning("DUPLICATE");
                        compatibility = RelationCompatibility.Redundant;
                    }
                    continue;

                case RelationCompatibility.Incompatible:
                    return RelationCompatibility.Incompatible;
            }
        }

        return compatibility;
    }
}