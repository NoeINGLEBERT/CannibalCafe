using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using System;
using UnityEngine.UIElements;

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

    public int TryAssignRole(PoltiRoleInstance roleInstance)
    {
        if (roleInstance == null)
            return -1;

        if (AssignedRoles == null)
            AssignedRoles = new List<PoltiRoleInstance>();

        if (Relations == null)
            Relations = new List<PoltiRelationInstance>();

        if (roleInstance.IsAssigned)
        {
            Debug.LogWarning($"Role {roleInstance.Template.Name} already assigned.");
            return -1;
        }

        if (AssignedRoles.Count >= 2)
        {
            return -1;
        }

        if (!roleInstance.Template.IsCompatible(this))
        {
            Debug.Log(AssignedRoles[0].Name + " Incompatible with role : " + roleInstance.Template.Name);
            return -1;
        }

        var relationsBackup = new List<PoltiRelationInstance>(Relations);

        int added = TryApplyRelations(roleInstance);

        if (added < 0)
        {
            Relations = relationsBackup;

            Debug.Log("Incompatible relations : " + roleInstance.Template.Name);

            return -1;
        }

        AssignedRoles.Add(roleInstance);
        roleInstance.AssignCharacter(this);

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

        return added;
    }

    private int TryApplyRelations(PoltiRoleInstance roleInstance)
    {
        int added = 0;

        foreach (var relation in roleInstance.Relations)
        {
            int r = TryAddRelation(relation);
            if (r < 0)
            {
                return -1;
            }

            added += r;
        }

        return added;
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

        if (!relation.IsCompatible(Relations))
        {
            return -1;
        }

        if (relation.Template is FamilialRelation newFamilial)
        {
            Relations.RemoveAll(existing =>
                existing.RoleTarget == relation.RoleTarget &&
                existing.Template is FamilialRelation existingFamilial &&
                existingFamilial.Type == FamilialRelationType.Unspecified
            );
        }

        Relations.Add(relation);
        added++;

        //if (relation.Template is FamilialRelation familial)
        //{
        //    int f = TrySpreadFamilialRelations(familial, relation);
        //    if (f < 0)
        //    {
        //        return -1;
        //    }

        //    added += f;
        //}

        return added;
    }

    //private int TrySpreadFamilialRelations(FamilialRelation familialTemplate, PoltiRelationInstance baseRelation)
    //{
    //    var targetCharacter = baseRelation.CharacterTarget;
    //    if (targetCharacter == null)
    //        return 0;

    //    int added = 0;

    //    foreach (var targetRelation in targetCharacter.Relations)
    //    {
    //        if (targetRelation.Template is not FamilialRelation targetFamilial)
    //            continue;

    //        var newRelationType = FamilialLogic.Resolve(
    //            familialTemplate.Type,
    //            targetFamilial.Type
    //        );

    //        var newTemplate = new FamilialRelation
    //        {
    //            Type = newRelationType,
    //            TargetRole = targetRelation.RoleTarget.Template
    //        };

    //        var newRelation = new PoltiRelationInstance(
    //            newTemplate,
    //            targetRelation.RoleTarget
    //        );

    //        int r = TryAddRelation(newRelation);
    //        if (r < 0)
    //        {
    //            return -1;
    //        }

    //        added += r;
    //    }

    //    return added;
    //}

    private int TrySpreadFamilialRelations(FamilialRelation familialTemplate, PoltiRelationInstance baseRelation)
    {
        int added = 0;

        List<PoltiRelationInstance> sourceRelations = null;

        CharacterConstraints targetCharacter = baseRelation.CharacterTarget;

        if (targetCharacter != null)
        {
            if (targetCharacter == this)
                return 0;

            sourceRelations = targetCharacter.Relations;
        }
        else if (baseRelation.RoleTarget != null)
        {
            if (!AssignedRoles.Contains(baseRelation.RoleTarget))
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

            if (targetCharacter != null)
            {
                newRelation.CharacterTarget = targetCharacter;
                // Add Reciprocal relation to target character
            }
            else
            {
                // Add Reciprocal relation to target role
            }


            int r = TryAddRelation(newRelation);
            if (r < 0)
            {
                return -1;
            }

            added += r;
        }

        return added;
    }
}

public class PoltiCharacterGenerator : MonoBehaviour
{
    [SerializeField] private int numberOfCharacters = 5;

    private PoltiSituationGenerator situationGenerator;

    private List<CharacterConstraints> generatedCharacters = new();

    public System.Action<List<CharacterConstraints>> OnCharactersGenerated;

    void Start()
    {
        situationGenerator = new PoltiSituationGenerator();
        GenerateCharacters(numberOfCharacters);
    }

    public void GenerateCharacters(int count)
    {
        generatedCharacters.Clear();

        for (int i = 0; i < count; i++)
        {
            GeneratedPoltiSituation generated = situationGenerator.GenerateRandomSituation();
            if (generated == null)
                continue;

            PoltiSituationInstance situationInstance =
                InstantiateSituation(generated);

            List<PoltiRoleInstance> aliveRoles = situationInstance.Roles
            .Where(r => !r.IsDead)
            .ToList();

            PoltiRoleInstance selectedRole =
            aliveRoles[UnityEngine.Random.Range(0, aliveRoles.Count)];

            CharacterConstraints character = new CharacterConstraints
            {
                Index = i,
                IsDead = false,
                AssignedSituation = situationInstance,
                AssignedRoles = new List<PoltiRoleInstance>(),
                Relations = new List<PoltiRelationInstance>()
            };

            character.TryAssignRole(selectedRole);

            generatedCharacters.Add(character);
        }

        Debug.Log($"Generated {generatedCharacters.Count} characters.");

        AssignLiveRoles();
        DebugAssignedRoles();
        OnCharactersGenerated?.Invoke(generatedCharacters);
    }

    public void AssignLiveRoles()
    {
        foreach (CharacterConstraints owner in generatedCharacters)
        {
            PoltiSituationInstance situation = owner.AssignedSituation;

            List<PoltiRoleInstance> unassignedRoles = situation.Roles
                .Where(r => !r.IsDead && !r.IsAssigned)
                .ToList();

            if (unassignedRoles.Count == 0)
                continue;

            // ---- STEP 1: Build candidate list per role ----

            Dictionary<PoltiRoleInstance, List<CharacterConstraints>> roleCandidates = new Dictionary<PoltiRoleInstance, List<CharacterConstraints>>();

            foreach (PoltiRoleInstance role in unassignedRoles)
            {
                List<CharacterConstraints> candidates = new List<CharacterConstraints>();

                foreach (CharacterConstraints character in generatedCharacters)
                {
                    if (character == owner)
                        continue;

                    if (SimulateAssignment(owner, character, role) >= 0)
                        candidates.Add(character);
                }

                if (candidates.Count == 0)
                {
                    Debug.Log("No candidates : " + role.Name);
                    continue;
                }

                roleCandidates[role] = candidates;
            }

            if (roleCandidates.Count == 0)
            {
                Debug.Log("No roleCandidates");
                continue;
            }

            // ---- STEP 2: Try all permutations ----

            List<PoltiRoleInstance> roles = roleCandidates.Keys.ToList();

            List<(CharacterConstraints character, PoltiRoleInstance role)> bestPermutation = FindBestPermutation(roles, roleCandidates, owner);

            if (bestPermutation == null)
            {
                Debug.Log("No bestPermutation");
                continue;
            }
                

            // ---- STEP 3: Apply for real ----

            foreach ((CharacterConstraints character, PoltiRoleInstance role) pair in bestPermutation)
                pair.character.TryAssignRole(pair.role);
        }
    }

    private int SimulateAssignment(CharacterConstraints sourceCharacter, CharacterConstraints targetCharacter, PoltiRoleInstance role)
    {
        List<CharacterConstraints> clonedCharacters = CloneCharacters(generatedCharacters);

        var clonedTargetCharacter = clonedCharacters[targetCharacter.Index];

        var clonedSourceCharacter = clonedCharacters[sourceCharacter.Index];

        var clonedRole = clonedSourceCharacter.AssignedSituation.Roles.FirstOrDefault(r => r.Template == role.Template);

        int added = clonedTargetCharacter.TryAssignRole(clonedRole);

        return added;
    }

    private List<(CharacterConstraints character, PoltiRoleInstance role)> FindBestPermutation(List<PoltiRoleInstance> roles, Dictionary<PoltiRoleInstance, List<CharacterConstraints>> candidates, CharacterConstraints sourceCharacter)
    {
        List<(CharacterConstraints, PoltiRoleInstance)> best = null;
        int bestCost = int.MaxValue;

        void Search(int index, int currentCost, List<(CharacterConstraints, PoltiRoleInstance)> current, HashSet<CharacterConstraints> used)
        {
            // all roles assigned
            if (index >= roles.Count)
            {
                if (currentCost < bestCost)
                {
                    bestCost = currentCost;
                    best = new List<(CharacterConstraints, PoltiRoleInstance)>(current);
                }
                return;
            }

            // pruning
            if (currentCost >= bestCost)
                return;

            var role = roles[index];

            foreach (var character in candidates[role])
            {
                if (used.Contains(character))
                    continue;

                // ---- Backup state ----
                var rolesBackup = new List<PoltiRoleInstance>(character.AssignedRoles);
                var relationsBackup = new List<PoltiRelationInstance>(character.Relations);

                int added = SimulateAssignment(sourceCharacter, character, role);

                if (added >= 0)
                {
                    used.Add(character);
                    current.Add((character, role));

                    Search(index + 1, currentCost + added, current, used);

                    current.RemoveAt(current.Count - 1);
                    used.Remove(character);
                }
            }
        }

        Search(0, 0, new List<(CharacterConstraints, PoltiRoleInstance)>(), new HashSet<CharacterConstraints>());

        return best;
    }

    private PoltiSituationInstance InstantiateSituation(GeneratedPoltiSituation generated)
    {
        PoltiSituationInstance instance = new PoltiSituationInstance
        {
            Template = generated.Source
        };

        foreach (var role in generated.Roles)
        {
            instance.Roles.Add(new PoltiRoleInstance(role));
        }

        foreach (var roleInstance in instance.Roles)
        {
            if (roleInstance.Template.Relations == null)
                continue;

            foreach (var relationTemplate in roleInstance.Template.Relations)
            {
                foreach (var targetRoleInstance in instance.Roles)
                {
                    if (targetRoleInstance.Template == relationTemplate.TargetRole && targetRoleInstance != roleInstance)
                    {
                        var relationInstance = new PoltiRelationInstance(
                            relationTemplate,
                            targetRoleInstance
                        );

                        roleInstance.Relations.Add(relationInstance);
                    }
                }
            }
        }

        return instance;
    }

    public void DebugAssignedRoles()
    {
        if (generatedCharacters == null || generatedCharacters.Count == 0)
        {
            Debug.Log("No characters generated.");
            return;
        }

        Debug.Log("===== Assigned Roles per Character =====");

        int charIndex = 1;
        foreach (var character in generatedCharacters)
        {
            string charInfo = $"{character.MinAge} - {character.MaxAge} | Character {charIndex} (Situation: {character.AssignedSituation?.Name ?? "None"}):";

            if (character.AssignedRoles != null && character.AssignedRoles.Count > 0)
            {
                charInfo += " Roles = [";
                charInfo += string.Join(", ", character.AssignedRoles.Select(r => r.Name));
                charInfo += "]";
            }
            else
            {
                charInfo += " No assigned roles.";
            }

            Debug.Log(charInfo);
            charIndex++;
        }

        Debug.Log("=======================================");
    }

    private List<CharacterConstraints> CloneCharacters(List<CharacterConstraints> originalCharacters)
    {
        var clonedCharacters = new List<CharacterConstraints>();

        // Mapping tables
        var situationMap = new Dictionary<PoltiSituationInstance, PoltiSituationInstance>();
        var roleMap = new Dictionary<PoltiRoleInstance, PoltiRoleInstance>();
        var characterMap = new Dictionary<CharacterConstraints, CharacterConstraints>();

        // =========================================================
        // 1. Clone Characters
        // =========================================================

        foreach (var original in originalCharacters)
        {
            var clone = new CharacterConstraints
            {
                Index = original.Index,
                IsDead = original.IsDead,
                MinAge = original.MinAge,
                MaxAge = original.MaxAge,
                AllowedGenders = (Gender[])original.AllowedGenders.Clone(),

                AssignedRoles = new List<PoltiRoleInstance>(),
                Relations = new List<PoltiRelationInstance>()
            };

            clonedCharacters.Add(clone);
            characterMap[original] = clone;
        }

        // =========================================================
        // 2. Clone Situations
        // =========================================================
        
        foreach (var original in originalCharacters)
        {
            var originalSituation = original.AssignedSituation;
            if (originalSituation == null)
                continue;

            if (!situationMap.ContainsKey(originalSituation))
            {
                var clonedSituation = new PoltiSituationInstance
                {
                    Template = originalSituation.Template
                };

                situationMap[originalSituation] = clonedSituation;
            }

            characterMap[original].AssignedSituation = situationMap[originalSituation];
        }


        // =========================================================
        // 3. Clone Roles of Situations
        // =========================================================

        foreach (var pair in situationMap)
        {
            var originalSituation = pair.Key;
            var clonedSituation = pair.Value;

            foreach (var originalRole in originalSituation.Roles)
            {
                var clonedRole = new PoltiRoleInstance(originalRole.Template);

                clonedSituation.Roles.Add(clonedRole);
                roleMap[originalRole] = clonedRole;
            }
        }

        // =========================================================
        // 4. Clone Relations of Roles
        // =========================================================

        foreach (var pair in roleMap)
        {
            var originalRole = pair.Key;
            var clonedRole = pair.Value;

            foreach (var originalRelation in originalRole.Relations)
            {
                if (!roleMap.TryGetValue(originalRelation.RoleTarget, out var clonedTargetRole))
                    continue;

                var clonedRelation = new PoltiRelationInstance(
                    originalRelation.Template,
                    clonedTargetRole
                );

                clonedRole.Relations.Add(clonedRelation);
            }
        }

        // =========================================================
        // 5. Assign Cloned Roles to Cloned Characters
        // =========================================================

        foreach (var original in originalCharacters)
        {
            var clone = characterMap[original];

            if (original.AssignedRoles != null)
            {
                foreach (var originalRole in original.AssignedRoles)
                {
                    if (!roleMap.TryGetValue(originalRole, out var clonedRole))
                        continue;

                    clone.TryAssignRole(clonedRole);
                }
            }
        }

        return clonedCharacters;
    }
}

