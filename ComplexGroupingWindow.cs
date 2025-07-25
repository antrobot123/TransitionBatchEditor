using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public static class ComplexGroupingConfig
{
    public static List<ConditionGroupingType> CurrentRules = new();
    public static void serialize() => EditorPrefs.SetString("TransitionEditor/ComplexGroupingRules", JsonUtility.ToJson(CurrentRules));
    public static void deserialize() => CurrentRules = JsonUtility.FromJson<List<ConditionGroupingType>>(EditorPrefs.GetString("TransitionEditor/ComplexGroupingRules"));
}
public class ComplexGroupingWindow : EditorWindow
{
    private List<ConditionGroupingType> selectedRules => ComplexGroupingConfig.CurrentRules;

    public static void ShowWindow()
    {
        var window = GetWindow<ComplexGroupingWindow>("Complex Grouping Setup");
        window.Show();
    }
    private void onEnable() => ComplexGroupingConfig.deserialize();
    private void onDestroy() => ComplexGroupingConfig.serialize();

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