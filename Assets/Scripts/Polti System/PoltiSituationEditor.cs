using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(PoltiSituation))]
public class PoltiSituationEditor : Editor
{
    private PoltiSituation situation;

    private int addNodeIndex = 0;
    private readonly string[] nodeOptions = { "Select Node...", "ROLE", "AND", "OR" };

    private const float elementHeight = 20f;
    private const float spacing = 4f;
    private const float leftMargin = 10f; // margin for wrapped lines

    private void OnEnable()
    {
        situation = (PoltiSituation)target;
        if (situation.RootNode == null)
            situation.RootNode = null;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Expression Tree", EditorStyles.boldLabel);

        Undo.RecordObject(situation, "Modify Expression Tree");

        Rect fullRect = GUILayoutUtility.GetRect(0, 10000, elementHeight, 10000);

        // Local variables for x and y
        float x = fullRect.x;
        float y = fullRect.y;

        DrawNodeFlow(ref situation.RootNode, fullRect, ref x, ref y, fullRect.width, true);

        if (GUI.changed)
            EditorUtility.SetDirty(situation);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawNodeFlow(ref ExpressionNode node, Rect parentRect, ref float x, ref float y, float maxWidth, bool isFirstLine)
    {
        float currentMargin = isFirstLine ? 0f : leftMargin;

        if (node == null)
        {
            Vector2 size = GUI.skin.button.CalcSize(new GUIContent(nodeOptions[0]));
            if (x + size.x > maxWidth)
            {
                x = parentRect.x + currentMargin;
                y += elementHeight + spacing;
                isFirstLine = false;
            }

            Rect popupRect = new Rect(x, y, Mathf.Min(size.x + 20, maxWidth - x), elementHeight);
            int selected = EditorGUI.Popup(popupRect, addNodeIndex, nodeOptions);
            if (selected != 0)
            {
                switch (selected)
                {
                    case 1: node = new RoleNode(); break;
                    case 2: node = new AndNode(); break;
                    case 3: node = new OrNode(); break;
                }
                addNodeIndex = 0;
            }
            x = popupRect.xMax + spacing;
            return;
        }

        // Role Node
        if (node is RoleNode roleNode)
        {
            float fieldWidth = 100f;
            if (x + fieldWidth > maxWidth)
            {
                x = parentRect.x + currentMargin;
                y += elementHeight + spacing;
                isFirstLine = false;
            }

            Rect fieldRect = new Rect(x, y, fieldWidth, elementHeight);
            roleNode.Role = (PoltiRole)EditorGUI.ObjectField(fieldRect, roleNode.Role, typeof(PoltiRole), false);
            x = fieldRect.xMax + spacing;

            Rect btnRect = new Rect(x, y, 20, elementHeight);
            if (GUI.Button(btnRect, "X"))
                node = null;
            x = btnRect.xMax + spacing;
        }
        // AND/OR Node
        else if (node is AndNode andNode || node is OrNode orNode)
        {
            string op = node is AndNode ? "&&" : "||";

            Vector2 parenSize = GUI.skin.label.CalcSize(new GUIContent("("));
            if (x + parenSize.x > maxWidth)
            {
                x = parentRect.x + currentMargin;
                y += elementHeight + spacing;
                isFirstLine = false;
            }
            Rect openRect = new Rect(x, y, parenSize.x, elementHeight);
            GUI.Label(openRect, "(");
            x += parenSize.x + spacing;

            List<ExpressionNode> children = node is AndNode ? ((AndNode)node).Children : ((OrNode)node).Children;

            for (int i = 0; i < children.Count; i++)
            {
                ExpressionNode child = children[i];
                DrawNodeFlow(ref child, parentRect, ref x, ref y, maxWidth, isFirstLine);
                children[i] = child;

                if (i < children.Count - 1)
                {
                    Vector2 opSize = GUI.skin.label.CalcSize(new GUIContent(op));
                    if (x + opSize.x > maxWidth)
                    {
                        x = parentRect.x + currentMargin;
                        y += elementHeight + spacing;
                        isFirstLine = false;
                    }
                    Rect opRect = new Rect(x, y, opSize.x, elementHeight);
                    GUI.Label(opRect, op);
                    x += opSize.x + spacing;
                }
            }

            // Add "+" button
            Vector2 plusSize = GUI.skin.button.CalcSize(new GUIContent("+"));
            if (x + plusSize.x > maxWidth)
            {
                x = parentRect.x + currentMargin;
                y += elementHeight + spacing;
                isFirstLine = false;
            }
            Rect plusRect = new Rect(x, y, plusSize.x, elementHeight);
            if (GUI.Button(plusRect, "+"))
                children.Add(null);
            x = plusRect.xMax + spacing;

            // Delete "X" button
            Vector2 delSize = GUI.skin.button.CalcSize(new GUIContent("X"));
            if (x + delSize.x > maxWidth)
            {
                x = parentRect.x + currentMargin;
                y += elementHeight + spacing;
                isFirstLine = false;
            }
            Rect delRect = new Rect(x, y, delSize.x, elementHeight);
            if (GUI.Button(delRect, "X"))
                node = null;
            x = delRect.xMax + spacing;

            // Closing parenthesis
            Vector2 closeSize = GUI.skin.label.CalcSize(new GUIContent(")"));
            if (x + closeSize.x > maxWidth)
            {
                x = parentRect.x + currentMargin;
                y += elementHeight + spacing;
                isFirstLine = false;
            }
            Rect closeRect = new Rect(x, y, closeSize.x, elementHeight);
            GUI.Label(closeRect, ")");
            x = closeRect.xMax + spacing;
        }
    }
}
