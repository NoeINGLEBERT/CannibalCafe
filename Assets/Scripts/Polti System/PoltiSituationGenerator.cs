using System.Collections.Generic;
using UnityEngine;

public class PoltiSituationGenerator
{
    private List<PoltiSituation> situations = new List<PoltiSituation>();

    public PoltiSituationGenerator()
    {
        LoadAllSituations();
    }

    private void LoadAllSituations()
    {
        situations.Clear();

        PoltiSituation[] loaded =
            Resources.LoadAll<PoltiSituation>("PoltiSituations");

        situations.AddRange(loaded);

        Debug.Log($"[PoltiService] Loaded {situations.Count} situations.");
    }

    /// <summary>
    /// Generates a random Polti situation and evaluates it fully.
    /// </summary>
    public GeneratedPoltiSituation GenerateRandomSituation()
    {
        if (situations.Count == 0)
            return null;

        PoltiSituation selected =
            situations[Random.Range(0, situations.Count)];

        if (selected.RootNode == null)
        {
            Debug.LogError($"Situation {selected.name} has no RootNode.");
            return null;
        }

        List<PoltiRole> roles = EvaluateNode(selected.RootNode);

        return new GeneratedPoltiSituation
        {
            Source = selected,
            Roles = roles
        };
    }

    private List<PoltiRole> EvaluateNode(ExpressionNode node)
    {
        switch (node.NodeType)
        {
            case NodeType.Role:
                return EvaluateRoleNode(node as RoleNode);

            case NodeType.And:
                return EvaluateAndNode(node as AndNode);

            case NodeType.Or:
                return EvaluateOrNode(node as OrNode);

            default:
                return new List<PoltiRole>();
        }
    }

    private List<PoltiRole> EvaluateRoleNode(RoleNode node)
    {
        if (node == null || node.Role == null)
            return new List<PoltiRole>();

        return new List<PoltiRole> { node.Role };
    }

    private List<PoltiRole> EvaluateAndNode(AndNode node)
    {
        List<PoltiRole> results = new List<PoltiRole>();

        foreach (var child in node.Children)
        {
            if (child == null)
                continue;

            results.AddRange(EvaluateNode(child));
        }

        return results;
    }

    private List<PoltiRole> EvaluateOrNode(OrNode node)
    {
        if (node.Children == null || node.Children.Count == 0)
            return new List<PoltiRole>();

        int index = Random.Range(0, node.Children.Count);

        var chosen = node.Children[index];

        if (chosen == null)
            return new List<PoltiRole>();

        return EvaluateNode(chosen);
    }
}
