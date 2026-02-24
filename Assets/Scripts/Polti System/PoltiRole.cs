using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;

public enum Gender
{
    Male,
    Female
}

public enum RelationCompatibility
{
    Compatible,
    Redundant,
    Incompatible
}

[Serializable]
public abstract class PoltiRelation
{
    public PoltiRole TargetRole;

    public abstract RelationCompatibility IsCompatible(PoltiRelation existingRelation, CharacterConstraints newTarget, CharacterConstraints existingTarget);
}

public enum FamilialRelationType
{
    Unspecified,
    Parent,
    Child,
    Grandparent,
    Grandchild,
    Sibling,
    Unrelated,
    Avuncular,
    Nibling,
    GrandAvuncular,
    GrandNibling,
    Cousin
}

[Serializable]
public class FamilialRelation : PoltiRelation
{
    public FamilialRelationType Type;

    public override RelationCompatibility IsCompatible(PoltiRelation existingRelation, CharacterConstraints newTarget, CharacterConstraints existingTarget)
    {
        if (existingRelation is not FamilialRelation other)
            return RelationCompatibility.Compatible;

        if (newTarget == existingTarget)
        {
            if (Type == FamilialRelationType.Unspecified && other.Type != FamilialRelationType.Unrelated)
                return RelationCompatibility.Compatible;
            if (Type == FamilialRelationType.Unrelated && other.Type != FamilialRelationType.Unspecified)
                return RelationCompatibility.Compatible;

            if (Type == other.Type)
            {
                return RelationCompatibility.Redundant;
            }

            return RelationCompatibility.Incompatible;
        }

        return RelationCompatibility.Compatible;
    }
}

public enum MaritalType
{
    None,
    Married,
    Divorced
}

[Serializable]
public class MaritalRelation : PoltiRelation
{
    public MaritalType Type;

    public override RelationCompatibility IsCompatible(PoltiRelation existingRelation, CharacterConstraints newTarget, CharacterConstraints existingTarget)
    {
        if (existingRelation is not MaritalRelation other)
            return RelationCompatibility.Compatible;

        if (newTarget == existingTarget)
        {
            if (Type == other.Type)
            {
                return RelationCompatibility.Redundant;
            }

            return RelationCompatibility.Incompatible;
        }
        else if (Type == MaritalType.Married && other.Type == MaritalType.Married)
        {
            return RelationCompatibility.Incompatible;
        }

        return RelationCompatibility.Compatible;
    }
}

public enum OutgoingType
{
    Love,
    Like,
    Hate,
    Rivalry
}

[Serializable]
public class OutgoingRelation : PoltiRelation
{
    public OutgoingType Type;

    public override RelationCompatibility IsCompatible(PoltiRelation existingRelation, CharacterConstraints newTarget, CharacterConstraints existingTarget)
    {
        if (existingRelation is not OutgoingRelation other)
            return RelationCompatibility.Compatible;

        if (newTarget == existingTarget)
        {
            if (Type == other.Type)
            {
                return RelationCompatibility.Redundant;
            }

            return RelationCompatibility.Incompatible;
        }
        else if ((Type == OutgoingType.Love && other.Type == OutgoingType.Love) ||
                (Type == OutgoingType.Rivalry && other.Type == OutgoingType.Rivalry))
        {
            return RelationCompatibility.Incompatible;
        }

        return RelationCompatibility.Compatible;
    }
}

public enum IncomingType
{
    Loved,
    Liked,
    Hated,
    Rivaled
}

[Serializable]
public class IncomingRelation : PoltiRelation
{
    public IncomingType Type;

    public override RelationCompatibility IsCompatible(PoltiRelation existingRelation, CharacterConstraints newTarget, CharacterConstraints existingTarget)
    {
        if (existingRelation is not IncomingRelation other)
            return RelationCompatibility.Compatible;

        if (newTarget == existingTarget)
        {
            if (Type == other.Type)
            {
                return RelationCompatibility.Redundant;
            }

            return RelationCompatibility.Incompatible;
        }

        return RelationCompatibility.Compatible;
    }
}

public enum CrimeType
{
    Murder,
    Murdered,
    Adultery,
}

[Serializable]
public class CrimeRelation : PoltiRelation
{
    public CrimeType Type;

    public override RelationCompatibility IsCompatible(PoltiRelation existingRelation, CharacterConstraints newTarget, CharacterConstraints existingTarget)
    {
        if (existingRelation is not CrimeRelation other)
            return RelationCompatibility.Compatible;

        if (newTarget == existingTarget)
        {
            if (Type == other.Type)
            {
                return RelationCompatibility.Redundant;
            }

            return RelationCompatibility.Incompatible;
        }

        return RelationCompatibility.Compatible;
    }
}

[CreateAssetMenu(menuName = "Polti/Role")]
public class PoltiRole : ScriptableObject
{
    public string Name;
    public bool IsDead;
    public int MinAge = 18;
    public int MaxAge = 81;
    public Gender[] AllowedGenders = { Gender.Male, Gender.Female };

    [SerializeReference]
    public List<PoltiRelation> Relations;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(Name))
            Name = name;
    }

    public bool IsCompatible(CharacterConstraints candidate)
    {
        if (IsDead != candidate.IsDead)
        {
            return false;
        }
        if (candidate.MaxAge < MinAge || candidate.MinAge > MaxAge)
        {
            return false;
        }

        bool genderMatch = Array.Exists(AllowedGenders, g => Array.Exists(candidate.AllowedGenders, cg => cg == g));
        if (!genderMatch)
        {
            return false;
        }

        return true;
    }

    public override string ToString() => Name;
}