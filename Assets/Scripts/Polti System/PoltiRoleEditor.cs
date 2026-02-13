using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(PoltiRole))]
public class PoltiRoleEditor : Editor
{
    private PoltiRole roleTarget;

    private void OnEnable()
    {
        roleTarget = (PoltiRole)target;

        // Make sure the list exists to avoid null references
        if (roleTarget.Relations == null)
            roleTarget.Relations = new System.Collections.Generic.List<PoltiRelation>();
    }

    public override void OnInspectorGUI()
    {
        // Draw default inspector for all fields, including Relations
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Add Relation", EditorStyles.boldLabel);

        // Buttons to add new relation types
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Familial"))
        {
            Undo.RecordObject(roleTarget, "Add Familial Relation");
            roleTarget.Relations.Add(Activator.CreateInstance<FamilialRelation>());
            EditorUtility.SetDirty(roleTarget);
        }
        if (GUILayout.Button("Outgoing"))
        {
            Undo.RecordObject(roleTarget, "Add Outgoing Relation");
            roleTarget.Relations.Add(Activator.CreateInstance<OutgoingRelation>());
            EditorUtility.SetDirty(roleTarget);
        }
        if (GUILayout.Button("Incoming"))
        {
            Undo.RecordObject(roleTarget, "Add Incoming Relation");
            roleTarget.Relations.Add(Activator.CreateInstance<IncomingRelation>());
            EditorUtility.SetDirty(roleTarget);
        }
        EditorGUILayout.EndHorizontal();
    }
}
