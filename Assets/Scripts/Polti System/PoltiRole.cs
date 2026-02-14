using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;

public enum Gender
{
    Male,
    Female
}

[Serializable]
public abstract class PoltiRelation
{
    public PoltiRole TargetRole;

    public abstract bool IsCompatible(List<PoltiRelation> existingRelations);
}

public enum FamilialRelationType
{
    Unspecified,
    Parent,
    Child,
    Grandparent,
    Grandchild,
    Sibling,
    Unrelated
}

[Serializable]
public class FamilialRelation : PoltiRelation
{
    public FamilialRelationType Type;

    public override bool IsCompatible(List<PoltiRelation> existingRelations)
    {
        foreach (var rel in existingRelations)
        {
            if (rel is FamilialRelation fr)
            {
                if (fr.Type == Type && fr.TargetRole != TargetRole)
                    return false;
            }
        }
        return true;
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

    public override bool IsCompatible(List<PoltiRelation> existingRelations)
    {
        return true;
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

    public override bool IsCompatible(List<PoltiRelation> existingRelations)
    {
        foreach (var rel in existingRelations)
        {
            if (rel is OutgoingRelation or)
            {
                if (or.TargetRole == TargetRole)
                {
                    if (or.Type != Type)
                        return false;
                }
            }
        }
        return true;
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

    public override bool IsCompatible(List<PoltiRelation> existingRelations)
    {
        foreach (var rel in existingRelations)
        {
            if (rel is IncomingRelation ir)
            {
                if (ir.TargetRole == TargetRole)
                {
                    if (ir.Type != Type)
                        return false;
                }
            }
        }
        return true;
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

    public override bool IsCompatible(List<PoltiRelation> existingRelations)
    {
        return true;
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
        // Set Name to asset name if empty
        if (string.IsNullOrEmpty(Name))
            Name = name; // 'name' is the asset’s filename
    }

    public bool IsCompatibleCandidate(PoltiRole candidate)
    {
        if (IsDead || candidate.IsDead) return false;
        if (candidate.MinAge < MinAge || candidate.MaxAge > MaxAge) return false;

        bool genderMatch = Array.Exists(AllowedGenders, g => Array.Exists(candidate.AllowedGenders, cg => cg == g));
        if (!genderMatch) return false;

        foreach (var rel in Relations)
        {
            if (!rel.IsCompatible(candidate.Relations))
                return false;
        }

        return true;
    }

    public override string ToString() => Name;
}