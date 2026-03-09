using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;

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

    public string GetDisplayName(CharacterConstraints viewer, List<VillagerData> villagers)
    {
        if (!IsDead)
        {
            return villagers[AssignedCharacter.Index].name;
        }

        return ResolveDeadName(viewer, villagers);
    }

    private string ResolveDeadName(CharacterConstraints viewer, List<VillagerData> villagers)
    {
        if (viewer != null)
        {
            var directRelation = outRelations
                .Where(r =>
                    r.CharacterTarget == viewer &&
                    r.Template is not CrimeRelation { Type: CrimeType.Murdered })
                .OrderBy(GetRelationPriority)
                .FirstOrDefault();

            if (directRelation != null)
            {
                bool viewerMale = villagers[viewer.Index].gender == Gender.Male.ToString();
                string possessive = viewerMale ? "his" : "her";

                string label = GetRelationLabel(directRelation, IsMale());
                return $"{possessive} {label}";
            }
        }

        var fallback = outRelations
            .Where(r =>
                r.Template is not CrimeRelation { Type: CrimeType.Murdered })
            .OrderBy(GetRelationPriority)
            .FirstOrDefault();

        if (fallback != null)
        {
            string otherName = villagers[fallback.CharacterTarget.Index].name;
            string label = GetRelationLabel(fallback, IsMale());

            return $"{otherName}'s {(label).ToLower()}";
        }

        return "an acquaintance";
    }

    private bool IsMale()
    {
        return Template.AllowedGenders[0] == Gender.Male;
    }

    private int GetRelationPriority(PoltiRelationInstance r)
    {
        return r.Template switch
        {
            FamilialRelation fam when fam.Type != FamilialRelationType.Unrelated => 0,
            MaritalRelation => 1,
            CrimeRelation => 2,
            IncomingRelation => 3,
            OutgoingRelation => 4,
            _ => 100
        };
    }

    private string GetRelationLabel(PoltiRelationInstance relation, bool male)
    {
        switch (relation.Template)
        {
            case FamilialRelation fam when fam.Type != FamilialRelationType.Unrelated:
                return GetFamilialLabel(fam.Type, male).ToLower();

            case MaritalRelation mar:
                return mar.Type switch
                {
                    MaritalType.Married => male ? "husband" : "wife",
                    MaritalType.Divorced => male ? "ex-husband" : "ex-wife",
                    _ => "acquaintance"
                };

            case IncomingRelation inRel:
                return inRel.Type switch
                {
                    IncomingType.Loved => "loved one",
                    IncomingType.Liked => "friend",
                    IncomingType.Hated => "enemy",
                    IncomingType.Rivaled => "rival",
                    _ => "acquaintance"
                };

            case OutgoingRelation outRel:
                return outRel.Type switch
                {
                    OutgoingType.Love => "loved one",
                    OutgoingType.Like => "friend",
                    OutgoingType.Hate => "enemy",
                    OutgoingType.Rivalry => "rival",
                    _ => "acquaintance"
                };

            case CrimeRelation crime:
                return crime.Type switch
                {
                    CrimeType.Adultery => male ? "lover" : "mistress",
                    _ => "acquaintance"
                };

            default:
                return "acquaintance";
        }
    }

    private string GetFamilialLabel(FamilialRelationType type, bool male)
    {
        return type switch
        {
            FamilialRelationType.Parent => male ? "father" : "mother",
            FamilialRelationType.Child => male ? "son" : "daughter",
            FamilialRelationType.Grandparent => male ? "grandfather" : "grandmother",
            FamilialRelationType.Grandchild => male ? "grandson" : "granddaughter",
            FamilialRelationType.Sibling => male ? "brother" : "sister",
            FamilialRelationType.Avuncular => male ? "uncle" : "aunt",
            FamilialRelationType.Nibling => male ? "nephew" : "niece",
            FamilialRelationType.GrandAvuncular => male ? "great-uncle" : "great-aunt",
            FamilialRelationType.GrandNibling => male ? "great-Nephew" : "great-niece",
            FamilialRelationType.Cousin => "cousin",
            _ => "relative" // PLACEHOLDER SHOULD BE REPLACED BY REAL STEP-PARENT DETECTION LOGIC
        };
    }
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