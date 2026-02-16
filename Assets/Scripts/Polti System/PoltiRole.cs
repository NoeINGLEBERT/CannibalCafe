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
        if (IsDead || candidate.IsDead)
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