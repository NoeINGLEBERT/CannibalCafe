using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CharacterConstraints
{
    public bool IsDead;
    public int MinAge = 18;
    public int MaxAge = 81;
    public Gender[] AllowedGenders = { Gender.Male, Gender.Female };

    public PoltiSituationInstance AssignedSituation;
    public List<PoltiRoleInstance> AssignedRoles;
    public List<PoltiRelationInstance> Relations;

    public void AssignRole(PoltiSituationInstance situation, PoltiRoleInstance roleInstance)
    {
        if (roleInstance == null)
            return;

        if (AssignedRoles == null)
            AssignedRoles = new List<PoltiRoleInstance>();

        if (Relations == null)
            Relations = new List<PoltiRelationInstance>();

        if (roleInstance.IsAssigned)
        {
            Debug.LogWarning($"Role {roleInstance.Template.Name} already assigned.");
            return;
        }

        AssignedRoles.Add(roleInstance);

        roleInstance.AssignCharacter(this);

        ApplyRelations(situation, roleInstance);
    }

    private void ApplyRelations(PoltiSituationInstance situation, PoltiRoleInstance roleInstance)
    {
        foreach (var relation in roleInstance.Relations)
        {
            if (!relation.IsCompatible(Relations))
                continue;

            TryAddRelation(relation);

            if (relation.Template is FamilialRelation familial)
            {
                SpreadFamilialRelations(familial, relation);
            }
        }
    }

    private void TryAddRelation(PoltiRelationInstance relation)
    {
        if (Relations.Exists(r => r.Template.GetType() == relation.Template.GetType() && r.RoleTarget == relation.RoleTarget))
            return;

        Relations.Add(relation);
    }

    private void SpreadFamilialRelations(FamilialRelation familialTemplate, PoltiRelationInstance baseRelation)
    {
        var targetCharacter = baseRelation.CharacterTarget;
        if (targetCharacter == null)
            return;

        foreach (var targetRelation in targetCharacter.Relations)
        {
            if (targetRelation.Template is not FamilialRelation targetFamilial)
                continue;

            var newRelationType = ResolveFamilialLogic(
                familialTemplate.Type,
                targetFamilial.Type
            );

            if (newRelationType == FamilialRelationType.Unspecified)
                continue;

            var newTemplate = new FamilialRelation
            {
                Type = newRelationType,
                TargetRole = targetRelation.RoleTarget.Template
            };

            var newRelation = new PoltiRelationInstance(
                newTemplate,
                targetRelation.RoleTarget
            );

            TryAddRelation(newRelation);
        }
    }

    private FamilialRelationType ResolveFamilialLogic(FamilialRelationType myRelation, FamilialRelationType theirRelation)
    {
        // Sibling + Parent = Avuncular
        if (myRelation == FamilialRelationType.Sibling &&
            theirRelation == FamilialRelationType.Parent)
            return FamilialRelationType.Avuncular;

        // Child + Sibling = Avuncular
        if (myRelation == FamilialRelationType.Child &&
            theirRelation == FamilialRelationType.Sibling)
            return FamilialRelationType.Avuncular;

        // Sibling + Child = Child
        if (myRelation == FamilialRelationType.Sibling &&
            theirRelation == FamilialRelationType.Child)
            return FamilialRelationType.Child;

        // Parent + Parent = Grandparent
        if (myRelation == FamilialRelationType.Parent &&
            theirRelation == FamilialRelationType.Parent)
            return FamilialRelationType.Grandparent;

        // Child + Child = Grandchild
        if (myRelation == FamilialRelationType.Child &&
            theirRelation == FamilialRelationType.Child)
            return FamilialRelationType.Grandchild;

        return FamilialRelationType.Unspecified;
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
            aliveRoles[Random.Range(0, aliveRoles.Count)];

            CharacterConstraints character = new CharacterConstraints
            {
                IsDead = false,
                AssignedSituation = situationInstance,
                AssignedRoles = new List<PoltiRoleInstance>(),
                Relations = new List<PoltiRelationInstance>()
            };

            character.AssignRole(situationInstance, selectedRole);

            generatedCharacters.Add(character);
        }

        Debug.Log($"Generated {generatedCharacters.Count} characters.");

        OnCharactersGenerated?.Invoke(generatedCharacters);
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
                    if (targetRoleInstance.Template == relationTemplate.TargetRole &&
                        targetRoleInstance != roleInstance)
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
}

