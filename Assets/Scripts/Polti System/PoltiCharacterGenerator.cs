using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PoltiCharacterGenerator : MonoBehaviour
{
    private PoltiSituationGenerator situationGenerator;

    private List<CharacterConstraints> generatedCharacters = new();

    public System.Action<List<CharacterConstraints>> OnCharactersGenerated;

    void Start()
    {
        situationGenerator = new PoltiSituationGenerator();
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

                    if (SimulateAssignment(owner, character, role).Success)
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
            {
                pair.character.TryAssignRole(pair.role);
            }

        }
    }

    private AssignmentResult SimulateAssignment(CharacterConstraints sourceCharacter, CharacterConstraints targetCharacter, PoltiRoleInstance role)
    {
        List<CharacterConstraints> clonedCharacters = CloneCharacters(generatedCharacters);

        var clonedTargetCharacter = clonedCharacters[targetCharacter.Index];

        var clonedSourceCharacter = clonedCharacters[sourceCharacter.Index];

        var clonedRole = clonedSourceCharacter.AssignedSituation.Roles.FirstOrDefault(r => r.Index == role.Index);

        AssignmentResult result = clonedTargetCharacter.TryAssignRole(clonedRole);

        return result;
    }

    private List<(CharacterConstraints character, PoltiRoleInstance role)> FindBestPermutation(List<PoltiRoleInstance> roles, Dictionary<PoltiRoleInstance, List<CharacterConstraints>> candidates, CharacterConstraints sourceCharacter)
    {
        List<List<(CharacterConstraints, PoltiRoleInstance)>> bests = new List<List<(CharacterConstraints, PoltiRoleInstance)>>();
        int bestCost = int.MaxValue;

        void Search(int index, int currentCost, List<(CharacterConstraints, PoltiRoleInstance)> current, HashSet<CharacterConstraints> used)
        {
            // all roles assigned
            if (index >= roles.Count)
            {
                if (currentCost <= bestCost)
                {
                    bestCost = currentCost;
                    List<(CharacterConstraints, PoltiRoleInstance)> best = new List<(CharacterConstraints, PoltiRoleInstance)>(current);
                    bests.Add(best);
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

                AssignmentResult result = SimulateAssignment(sourceCharacter, character, role);

                if (result.Success)
                {
                    used.Add(character);
                    current.Add((character, role));

                    Search(index + 1, currentCost + result.DeltaAssignedRelation, current, used);

                    current.RemoveAt(current.Count - 1);
                    used.Remove(character);
                }
            }
        }

        Search(0, 0, new List<(CharacterConstraints, PoltiRoleInstance)>(), new HashSet<CharacterConstraints>());

        return bests[UnityEngine.Random.Range(0, bests.Count)];
    }

    private PoltiSituationInstance InstantiateSituation(GeneratedPoltiSituation generated)
    {
        PoltiSituationInstance instance = new PoltiSituationInstance
        {
            Template = generated.Source
        };

        List<int> usedIndexes = new List<int>();
        foreach (PoltiRole role in generated.Roles)
        {
            int index = instance.Template.Id * 100 + role.Index * 10;

            while (usedIndexes.Contains(index))
            {
                index++;
            }

            instance.Roles.Add(new PoltiRoleInstance(index, role, instance));
            usedIndexes.Add(index);
        }

        foreach (PoltiRoleInstance roleInstance in instance.Roles)
        {
            if (roleInstance.Template.Relations == null)
                continue;

            foreach (PoltiRelation relationTemplate in roleInstance.Template.Relations)
            {
                foreach (PoltiRoleInstance targetRoleInstance in instance.Roles)
                {
                    if (targetRoleInstance.Template == relationTemplate.TargetRole && targetRoleInstance != roleInstance)
                    {
                        PoltiRelationInstance relationInstance = new PoltiRelationInstance(
                            relationTemplate,
                            targetRoleInstance
                        );

                        roleInstance.outRelations.Add(relationInstance);
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
                var clonedRole = new PoltiRoleInstance(originalRole.Index, originalRole.Template, clonedSituation);

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

            foreach (var originalRelation in originalRole.outRelations)
            {
                if (!roleMap.TryGetValue(originalRelation.RoleTarget, out var clonedTargetRole))
                    continue;

                var clonedRelation = new PoltiRelationInstance(
                    originalRelation.Template,
                    clonedTargetRole
                );

                clonedRole.outRelations.Add(clonedRelation);
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

