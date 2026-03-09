using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VillagerNarrativeBuilder : MonoBehaviour
{
    public void ApplySituations(
        List<VillagerData> villagers,
        List<CharacterConstraints> constraints)
    {
        for (int i = 0; i < constraints.Count; i++)
        {
            var c = constraints[i];
            var v = villagers[i];

            string situations = "";

            foreach (var role in c.AssignedRoles)
            {
                situations += "-" + ResolveSentence(role, villagers) + "\n";
            }

            v.situations = situations;
        }
    }

    public void ApplyRelations(
        List<VillagerData> villagers,
        List<CharacterConstraints> constraints)
    {
        for (int i = 0; i < constraints.Count; i++)
        {
            CharacterConstraints c = constraints[i];
            VillagerData v = villagers[i];
            List<PoltiRoleInstance> deadRoles = new List<PoltiRoleInstance>();

            foreach (PoltiRelationInstance r in c.Relations)
            {
                CharacterConstraints target = r.CharacterTarget;

                if (target == null)
                {
                    if (!deadRoles.Contains(r.RoleTarget))
                    {
                        PoltiRelationInstance murderedRelation = r.RoleTarget.outRelations.FirstOrDefault(rel => rel.Template is CrimeRelation cr && cr.Type == CrimeType.Murdered);

                        if (murderedRelation == null)
                            continue;

                        if (murderedRelation.CharacterTarget == c)
                        {
                            v.relations += "-Murdered " + r.RoleTarget.GetDisplayName(c, villagers) + "\n";
                            deadRoles.Add(r.RoleTarget);
                        }
                        else
                        {
                            v.relations += "-" + villagers[murderedRelation.CharacterTarget.Index].name + " murdered " + r.RoleTarget.GetDisplayName(c, villagers) + "\n";
                            deadRoles.Add(r.RoleTarget);
                        }
                    }

                    continue;
                }

                VillagerData targetVillager = villagers[target.Index];

                string relationText = "";

                // =====================================================
                // Familial relations
                // =====================================================
                if (r.Template is FamilialRelation fam)
                {
                    string label = GetFamilialLabel(fam.Type, v.gender == "Male");
                    relationText = $"{targetVillager.name}'s {label}";
                }

                // =====================================================
                // Marriage
                // =====================================================
                else if (r.Template is MaritalRelation mar)
                {
                    switch (mar.Type)
                    {
                        case MaritalType.Married:
                            relationText = $"Married to {targetVillager.name}";
                            break;
                        case MaritalType.Divorced:
                            relationText = $"Divorced {targetVillager.name}";
                            break;
                    }
                }

                // =====================================================
                // Outgoing feelings
                // =====================================================
                else if (r.Template is OutgoingRelation outRel)
                {
                    relationText = $"{outRel.Type} : {targetVillager.name}";

                    switch (outRel.Type)
                    {
                        case OutgoingType.Love:
                            relationText = $"In love with {targetVillager.name}";
                            break;
                        case OutgoingType.Like:
                            relationText = $"Friends with {targetVillager.name}";
                            break;
                        case OutgoingType.Hate:
                            relationText = $"Hates {targetVillager.name}";
                            break;
                        case OutgoingType.Rivalry:
                            relationText = $"Rivals with {targetVillager.name}";
                            break;
                    }
                }

                // =====================================================
                // Incoming feelings
                // =====================================================
                else if (r.Template is IncomingRelation inRel)
                {
                    relationText = $"{inRel.Type} : {targetVillager.name}";

                    switch (inRel.Type)
                    {
                        case IncomingType.Loved:
                            relationText = $"Loved by {targetVillager.name}";
                            break;
                        case IncomingType.Liked:
                            relationText = $"{targetVillager.name}'s friend";
                            break;
                        case IncomingType.Hated:
                            relationText = $"Hated by {targetVillager.name}";
                            break;
                        case IncomingType.Rivaled:
                            relationText = $"{targetVillager.name}'s rival";
                            break;
                    }
                }

                // =====================================================
                // Crimes
                // =====================================================
                else if (r.Template is CrimeRelation crime)
                {
                    switch (crime.Type)
                    {
                        case CrimeType.Murder:
                            relationText = $"Murdered {targetVillager.name}";
                            break;
                        case CrimeType.Adultery:
                            relationText = $"Had an affair with {targetVillager.name}";
                            break;
                    }
                }

                // =====================================================
                // Append readable text
                // =====================================================
                if (!string.IsNullOrEmpty(relationText))
                {
                    v.relations += "-" + relationText + "\n";
                }
            }
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

    private string ResolveSentence(
        PoltiRoleInstance role,
        List<VillagerData> villagers)
    {
        if (string.IsNullOrEmpty(role.Template.Sentence))
            return role.Template.Sentence;

        return System.Text.RegularExpressions.Regex.Replace(
            role.Template.Sentence,
            @"\{(\dX|\d{2})\}",
            match =>
            {
                string token = match.Groups[1].Value;

                int currentUnits = role.Index % 10;

                foreach (var r in role.Situation.Roles)
                {
                    int lastTwoDigits = r.Index % 100;
                    int tens = lastTwoDigits / 10;
                    int units = lastTwoDigits % 10;

                    // Normal case: {01}, {12}, etc.
                    if (token.Length == 2 && char.IsDigit(token[1]))
                    {
                        if (lastTwoDigits.ToString("D2") == token)
                        {
                            return r.GetDisplayName(
                                role.AssignedCharacter,
                                villagers
                            );
                        }
                    }
                    // Special case: {1X}
                    else if (token.Length == 2 && token[1] == 'X')
                    {
                        int tokenTens = token[0] - '0';

                        if (tens == tokenTens && units != currentUnits)
                        {
                            return r.GetDisplayName(
                                role.AssignedCharacter,
                                villagers
                            );
                        }
                    }
                }

                Debug.LogWarning($"No role found for token {{{token}}}");
                return match.Value; // fallback
            });
    }
}
