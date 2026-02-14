using UnityEngine;
using System.Collections.Generic;

public class CharacterConstraints
{
    public bool IsDead;
    public int MinAge = 18;
    public int MaxAge = 81;
    public Gender[] AllowedGenders = { Gender.Male, Gender.Female };

    public PoltiSituationInstance AssignedSituation;
    public List<PoltiRoleInstance> AssignedRoles;
}

public class PoltiCharacterGenerator : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
