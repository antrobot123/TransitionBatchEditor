using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public static class ComplexGroupingConfig
{
    public static List<ConditionGroupingType> CurrentRules = new();
}
public class ComplexGroupingWindow : EditorWindow
{
    private List<ConditionGroupingType> selectedRules => ComplexGroupingConfig.CurrentRules;

    public static void ShowWindow()
    {
        var window = GetWindow<ComplexGroupingWindow>("Complex Grouping Setup");
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Complex Grouping Rules", EditorStyles.boldLabel);
        
        for (int i = 0; i < selectedRules.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            selectedRules[i] = (ConditionGroupingType)EditorGUILayout.EnumPopup(selectedRules[i]);

            if (GUILayout.Button("↑", GUILayout.Width(30)) && i > 0)
            {
                (selectedRules[i - 1], selectedRules[i]) = (selectedRules[i], selectedRules[i - 1]);
            }

            if (GUILayout.Button("↓", GUILayout.Width(30)) && i < selectedRules.Count - 1)
            {
                (selectedRules[i + 1], selectedRules[i]) = (selectedRules[i], selectedRules[i + 1]);
            }

            if (GUILayout.Button("✖", GUILayout.Width(30)))
            {
                selectedRules.RemoveAt(i);
                break;
            }

            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Add Rule"))
            selectedRules.Add(ConditionGroupingType.ParameterName);

        if (GUILayout.Button("Apply"))
        {
            // TODO: Send selectedRules back to main window
            Close();
        }
    }
}