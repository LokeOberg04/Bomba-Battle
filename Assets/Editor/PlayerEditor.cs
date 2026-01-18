using UnityEngine;
using UnityEditor;
using UnityEditor.TerrainTools;

[CustomEditor(typeof(Player))]
public class PlayerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        Player player = target as Player;

        float health = player.health.Value;

        using (new EditorGUI.DisabledGroupScope(true))
        {
            EditorGUILayout.FloatField("Current Health", health);
        }

        ActionStack stack = target as ActionStack;
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Action Stack", EditorStyles.boldLabel);
        GUILayout.BeginVertical(EditorStyles.helpBox);
        foreach (ActionStack.IAction evt in stack.Stack)
        {
            string name = "   #" + stack.Stack.IndexOf(evt) + ": " + evt.ToString();
            if (evt is Object obj)
            {
                if (GUILayout.Button(name, evt == stack.CurrentAction ? EditorStyles.boldLabel : EditorStyles.label))
                {
                    Selection.activeObject = obj;
                }
            }
            else
            {
                EditorGUILayout.LabelField(name, evt == stack.CurrentAction ? EditorStyles.boldLabel : EditorStyles.label);
            }
        }
        GUILayout.EndVertical();

    }
}
