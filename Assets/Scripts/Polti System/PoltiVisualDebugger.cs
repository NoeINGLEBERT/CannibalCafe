using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PoltiGraphVisualizer : MonoBehaviour
{
    [SerializeField] private PoltiCharacterGenerator generator;
    [SerializeField] private float radius = 5f;

    private List<CharacterConstraints> characters;

    private void OnEnable()
    {
        generator.OnCharactersGenerated += CacheData;
    }

    private void OnDisable()
    {
        generator.OnCharactersGenerated -= CacheData;
    }

    private void CacheData(List<CharacterConstraints> generated)
    {
        characters = generated;
    }

    private void OnDrawGizmos()
    {
        if (characters == null || characters.Count == 0)
            return;

        List<NodeData> nodes = BuildNodeList();

        float angleStep = 360f / nodes.Count;

        for (int i = 0; i < nodes.Count; i++)
        {
            float angle = Mathf.Deg2Rad * (angleStep * i);
            Vector3 pos = new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0f);

            nodes[i].Position = transform.position + pos;
        }

        DrawEdges(nodes);
        DrawNodes(nodes);
    }

    private List<NodeData> BuildNodeList()
    {
        List<NodeData> nodes = new();
        int index = 0;

        foreach (var character in characters)
        {
            nodes.Add(new NodeData
            {
                Index = index++,
                Character = character,
                Role = character.AssignedRoles.First(),
                IsAssigned = true
            });

            foreach (var role in character.AssignedSituation.Roles)
            {
                if (!role.IsAssigned)
                {
                    nodes.Add(new NodeData
                    {
                        Index = index++,
                        Character = null,
                        Role = role,
                        IsAssigned = false
                    });
                }
            }
        }

        return nodes;
    }

    private void DrawNodes(List<NodeData> nodes)
    {
        float nodeSize = radius * 0.08f;

        foreach (var node in nodes)
        {
            Gizmos.color = node.IsAssigned ? Color.cyan : Color.gray;
            Gizmos.DrawSphere(node.Position, nodeSize);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                node.Position + Vector3.up * nodeSize * 1.5f,
                node.Role.Name);
#endif
        }
    }

    private void DrawEdges(List<NodeData> nodes)
    {
        // Track relations between same node pairs
        Dictionary<(NodeData, NodeData), List<PoltiRelationInstance>> edgeGroups = new();

        foreach (var node in nodes)
        {
            if (!node.IsAssigned)
                continue;

            foreach (var relation in node.Character.Relations)
            {
                NodeData targetNode = nodes.FirstOrDefault(n =>
                    n.Role == relation.RoleTarget);

                if (targetNode == null)
                    continue;

                var key = node.Index < targetNode.Index
                    ? (node, targetNode)
                    : (targetNode, node);

                if (!edgeGroups.ContainsKey(key))
                    edgeGroups[key] = new List<PoltiRelationInstance>();

                edgeGroups[key].Add(relation);
            }
        }

        float spacing = radius * 0.08f;

        foreach (var pair in edgeGroups)
        {
            NodeData a = pair.Key.Item1;
            NodeData b = pair.Key.Item2;
            var relations = pair.Value;

            Vector3 dir = (b.Position - a.Position).normalized;
            Vector3 perpendicular = new Vector3(-dir.y, dir.x, 0f);

            int count = relations.Count;

            for (int i = 0; i < count; i++)
            {
                float offsetIndex = i - (count - 1) / 2f;
                Vector3 offset = perpendicular * offsetIndex * spacing;

                Vector3 start = a.Position + offset;
                Vector3 end = b.Position + offset;

                Gizmos.color = Color.white;
                Gizmos.DrawLine(start, end);

#if UNITY_EDITOR
                Vector3 mid = (start + end) / 2f;
                UnityEditor.Handles.Label(mid, GetRelationEnumName(relations[i]));
#endif
            }
        }
    }


    private string GetRelationEnumName(PoltiRelationInstance relation)
    {
        var template = relation.Template;

        switch (template)
        {
            case FamilialRelation fr:
                return fr.Type.ToString();

            case MaritalRelation mr:
                return mr.Type.ToString();

            case OutgoingRelation or:
                return or.Type.ToString();

            case IncomingRelation ir:
                return ir.Type.ToString();

            case CrimeRelation cr:
                return cr.Type.ToString();

            default:
                return "Relation";
        }
    }

    private class NodeData
    {
        public int Index; // stable ordering
        public CharacterConstraints Character;
        public PoltiRoleInstance Role;
        public bool IsAssigned;
        public Vector3 Position;
    }
}
