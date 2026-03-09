using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using System;

public class AssignmentResult
{
    public bool Success;
    public int DeltaAssignedRelation;
    public int DeltaUnassignedRelation;

    public AssignmentResult(bool success, int deltaAssignedRelation = 0, int deltaUnassignedRelation = 0)
    {
        Success = success;
        DeltaAssignedRelation = deltaAssignedRelation;
        DeltaUnassignedRelation = deltaUnassignedRelation;
    }
}

public class CharacterConstraints
{
    public int Index;

    public bool IsDead;
    public int MinAge = 18;
    public int MaxAge = 81;
    public Gender[] AllowedGenders = { Gender.Male, Gender.Female };

    public PoltiSituationInstance AssignedSituation;
    public List<PoltiRoleInstance> AssignedRoles;
    public List<PoltiRelationInstance> Relations;

    public AssignmentResult TryAssignRole(PoltiRoleInstance roleInstance)
    {
        if (roleInstance == null)
            return new AssignmentResult(false);

        if (AssignedRoles == null)
            AssignedRoles = new List<PoltiRoleInstance>();

        if (Relations == null)
            Relations = new List<PoltiRelationInstance>();

        if (roleInstance.IsAssigned)
        {
            Debug.LogWarning($"Role {roleInstance.Template.Name} already assigned.");
            return new AssignmentResult(false);
        }

        if (AssignedRoles.Count >= 2)
        {
            return new AssignmentResult(false);
        }

        if (!roleInstance.Template.IsCompatible(this))
        {
            Debug.Log(AssignedRoles[0].Name + " Incompatible with role : " + roleInstance.Template.Name);
            return new AssignmentResult(false);
        }

        List<PoltiRelationInstance> relationsBackup = new List<PoltiRelationInstance>(Relations);

        AssignmentResult relationsResult = TryApplyRelations(roleInstance);

        if (!relationsResult.Success)
        {
            Relations = relationsBackup;

            Debug.Log("Incompatible relations : " + roleInstance.Template.Name);

            return new AssignmentResult(false);
        }

        AssignedRoles.Add(roleInstance);
        AssignmentResult roleResult = roleInstance.AssignCharacter(this);

        if (!roleResult.Success)
        {
            return new AssignmentResult(false);
        }

        int intersectMinAge = Mathf.Max(MinAge, roleInstance.Template.MinAge);
        int intersectMaxAge = Mathf.Min(MaxAge, roleInstance.Template.MaxAge);

        List<Gender> intersectGenders = new List<Gender>();
        foreach (Gender g in AllowedGenders)
        {
            if (Array.Exists(roleInstance.Template.AllowedGenders, rg => rg == g))
                intersectGenders.Add(g);
        }

        MinAge = intersectMinAge;
        MaxAge = intersectMaxAge;
        AllowedGenders = intersectGenders.ToArray();

        return new AssignmentResult(true, relationsResult.DeltaAssignedRelation + roleResult.DeltaAssignedRelation, relationsResult.DeltaUnassignedRelation + roleResult.DeltaUnassignedRelation);
    }

    private AssignmentResult TryApplyRelations(PoltiRoleInstance roleInstance)
    {
        int addedAssigned = 0;
        int addedUnassigned = 0;

        foreach (var relation in roleInstance.outRelations)
        {
            AssignmentResult r = TryAddRelation(relation);
            if (!r.Success)
            {
                return new AssignmentResult(false);
            }

            addedAssigned += r.DeltaAssignedRelation;
            addedUnassigned += r.DeltaUnassignedRelation;
        }

        return new AssignmentResult(true, addedAssigned, addedUnassigned);
    }

    private AssignmentResult TryAddRelation(PoltiRelationInstance relation)
    {
        int addedAssigned = 0;
        int addedUnassigned = 0;

        switch (relation.IsCompatible(Relations))
        {
            case RelationCompatibility.Redundant:
                return new AssignmentResult(true);

            case RelationCompatibility.Incompatible:
                return new AssignmentResult(false);
        }

        Relations.Add(relation);
        relation.assignedCharacter = this;

        if (relation.RoleTarget.IsAssigned)
        {
            addedAssigned++;
        }
        else
        {
            addedUnassigned++;
        }

        //if (relation.Template is FamilialRelation familial)
        //{
        //    int f = TrySpreadFamilialRelations(familial, relation);
        //    if (f < 0)
        //    {
        //        return -1;
        //    }

        //    added += f;
        //}

        return new AssignmentResult(true, addedAssigned, addedUnassigned);
    }

    private AssignmentResult TrySpreadFamilialRelations(FamilialRelation familialTemplate, PoltiRelationInstance baseRelation)
    {
        int addedAssigned = 0;
        int addedUnassigned = 0;

        List<PoltiRelationInstance> sourceRelations = null;

        CharacterConstraints targetCharacter = baseRelation.CharacterTarget;

        if (targetCharacter != null)
        {
            if (targetCharacter == this)
                return new AssignmentResult(true);

            sourceRelations = targetCharacter.Relations;
        }
        else if (baseRelation.RoleTarget != null)
        {
            if (!AssignedRoles.Contains(baseRelation.RoleTarget))
                return new AssignmentResult(true);

            sourceRelations = baseRelation.RoleTarget.outRelations;
        }
        else
        {
            return new AssignmentResult(true);
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

            if (targetCharacter != null)
            {
                newRelation.CharacterTarget = targetCharacter;
                // Add Reciprocal relation to target character
            }
            else
            {
                // Add Reciprocal relation to target role
            }


            AssignmentResult r = TryAddRelation(newRelation);
            if (!r.Success)
            {
                return new AssignmentResult(false, 0);
            }

            addedAssigned += r.DeltaAssignedRelation;
        }

        return new AssignmentResult(true, addedAssigned, addedUnassigned);
    }
}