using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Polti/Situation")]
public class PoltiSituation : ScriptableObject
{
    public string Id;
    public string Category;
    [SerializeReference]
    public ExpressionNode RootNode;
}

public enum NodeType { Role, And, Or }

[System.Serializable]
public abstract class ExpressionNode
{
    public NodeType NodeType;
}

[System.Serializable]
public class RoleNode : ExpressionNode
{
    public PoltiRole Role;

    public RoleNode() { NodeType = NodeType.Role; }
}

[System.Serializable]
public class AndNode : ExpressionNode
{
    [SerializeReference]
    public List<ExpressionNode> Children = new List<ExpressionNode>();

    public AndNode()
    {
        NodeType = NodeType.And;
        Children = new List<ExpressionNode> { null, null };
    }
}

[System.Serializable]
public class OrNode : ExpressionNode
{
    [SerializeReference]
    public List<ExpressionNode> Children = new List<ExpressionNode>();

    public OrNode()
    {
        NodeType = NodeType.Or;
        Children = new List<ExpressionNode> { null, null };
    }
}