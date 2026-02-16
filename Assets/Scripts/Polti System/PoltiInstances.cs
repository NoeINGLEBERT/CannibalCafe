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
        IsAssigned = character != null;

        OnRoleAssigned?.Invoke(character);
    }

    private int TryAddRelation(PoltiRelationInstance relation)
    {
        int added = 0;

        foreach (var existing in Relations)
        {
            if (existing.RoleTarget == relation.RoleTarget && relation.HaveSameRelationType(existing))
            {
                return 0;
            }
        }

        if (relation.Template is FamilialRelation newFamilial && newFamilial.Type != FamilialRelationType.Unspecified)
        {
            Relations.RemoveAll(existing =>
                existing.RoleTarget == relation.RoleTarget &&
                existing.Template is FamilialRelation existingFamilial &&
                existingFamilial.Type == FamilialRelationType.Unspecified
            );
        }

        if (!relation.IsCompatible(Relations))
        {
            return -1;
        }

        Relations.Add(relation);
        added++;

        if (relation.Template is FamilialRelation familial)
        {
            int f = TrySpreadFamilialRelations(familial, relation);
            if (f < 0)
            {
                return -1;
            }

            added += f;
        }

        return added;
    }

    private int TrySpreadFamilialRelations(FamilialRelation familialTemplate, PoltiRelationInstance baseRelation)
    {
        int added = 0;

        List<PoltiRelationInstance> sourceRelations = null;

        CharacterConstraints targetCharacter = baseRelation.CharacterTarget;

        if (targetCharacter != null)
        {
            if (targetCharacter == AssignedCharacter)
                return 0;

            sourceRelations = targetCharacter.Relations;
        }
        else if (baseRelation.RoleTarget != null)
        {
            if (baseRelation.RoleTarget == this)
                return 0;

            sourceRelations = baseRelation.RoleTarget.Relations;
        }
        else
        {
            return 0;
        }

        foreach (PoltiRelationInstance targetRelation in sourceRelations)
        {
            if (targetRelation.Template is not FamilialRelation targetFamilial)
                continue;

            FamilialRelationType newRelationType = FamilialLogic.Resolve(
                familialTemplate.Type,
                targetFamilial.Type
            );

            FamilialRelation newTemplate = new FamilialRelation
            {
                Type = newRelationType,
                TargetRole = targetRelation.RoleTarget.Template
            };

            PoltiRelationInstance newRelation = new PoltiRelationInstance(
                newTemplate,
                targetRelation.RoleTarget
            );

            newRelation.CharacterTarget = targetCharacter;

            int r = TryAddRelation(newRelation);
            if (r < 0)
            {
                return -1;
            }

            added += r;
        }

        return added;
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
        foreach (var existing in existingRelations)
        {
            // Only compare same type of relation
            if (existing.Template.GetType() != Template.GetType())
                continue;

            // If it's an exact replica (same RoleTarget and same type/enum), skip it
            if (existing.RoleTarget == RoleTarget && HaveSameRelationType(existing))
                continue; // ignore, will be trimmed later

            // Handle CrimeRelation: block Murdered duplicates
            if (Template is CrimeRelation crimeTemplate && existing.Template is CrimeRelation existingCrime)
            {
                if (crimeTemplate.Type == CrimeType.Murdered && existingCrime.Type == CrimeType.Murdered)
                    return false;
                continue; // other crime types can overlap
            }

            // Handle FamilialRelation: allow if existing is Unspecified and new is not Unrelated
            if (Template is FamilialRelation familialTemplate && existing.Template is FamilialRelation existingFamilial)
            {
                if (existingFamilial.Type == FamilialRelationType.Unspecified && familialTemplate.Type != FamilialRelationType.Unrelated)
                    continue; // don’t block, let template decide
                if (familialTemplate.Type == FamilialRelationType.Unspecified && existingFamilial.Type != FamilialRelationType.Unrelated)
                    continue; // don’t block, let template decide
            }

            // Handle MaritalRelation: only one spouse allowed
            if (Template is MaritalRelation maritalTemplate && existing.Template is MaritalRelation existingMarital)
            {
                if (maritalTemplate.Type == MaritalType.Married || existingMarital.Type == MaritalType.Married)
                    return false; // cannot be married to multiple partners
            }

            // Handle OutgoingRelation: only one Love or Rivalry target allowed
            if (Template is OutgoingRelation outgoingTemplate && existing.Template is OutgoingRelation existingOutgoing)
            {
                if ((outgoingTemplate.Type == OutgoingType.Love && existingOutgoing.Type == OutgoingType.Love) ||
                    (outgoingTemplate.Type == OutgoingType.Rivalry && existingOutgoing.Type == OutgoingType.Rivalry))
                {
                    return false; // cannot be in love or rivalry with multiple people
                }
            }

            // Default: same RoleTarget blocks other conflicts
            if (existing.RoleTarget == RoleTarget)
            {
                if (Template is FamilialRelation a && existing.Template is FamilialRelation b)
                {
                    Debug.Log("Conflicting relations : " + a.Type + " / " + b.Type);
                }

                return false;
            }
        }

        return true;
    }

    public bool HaveSameRelationType(PoltiRelationInstance existing)
    {
        return (Template, existing.Template) switch
        {
            (FamilialRelation a, FamilialRelation b) => a.Type == b.Type,
            (MaritalRelation a, MaritalRelation b) => a.Type == b.Type,
            (OutgoingRelation a, OutgoingRelation b) => a.Type == b.Type,
            (IncomingRelation a, IncomingRelation b) => a.Type == b.Type,
            (CrimeRelation a, CrimeRelation b) => a.Type == b.Type,
            _ => false
        };
    }
}