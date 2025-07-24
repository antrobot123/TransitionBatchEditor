using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class TransitionConditionSplitterWindow : EditorWindow
{
    private BulkSelectionMode bulkMode = BulkSelectionMode.SelectedOnly;
    private List<string> specificLayerNames = new();
    private BulkSelectionMode lastBulkMode = BulkSelectionMode.SelectedOnly;


    private AnimatorTransitionBase[] selectedTransitions;
    private List<ConditionRow> conditionRows = new();
    private ConditionGroupingType selectedGrouping = ConditionGroupingType.ComparisonMode;
    private Dictionary<string, AnimatorControllerParameterType> parameterTypeMap = new();
    Vector2 scrollPos;


    [MenuItem("Tools/Condition Splitting Window")]
    public static void ShowWindow()
    {
        GetWindow<TransitionConditionSplitterWindow>("Condition Splitting");
    }

    private void OnFocus() => RefreshSelection();
    private void OnSelectionChange()
    {
        RefreshSelection();
        Repaint();
    }

    private bool ShowSelectionWarning()
    {
        bool needsAnimator = bulkMode is BulkSelectionMode.AllLayers or BulkSelectionMode.SpecificLayers;
        bool needsTransition = bulkMode is BulkSelectionMode.SelectedOnly;

        bool hasAnimator = ResolveControllerFromSelection() != null;
        bool hasTransition = Selection.objects.OfType<AnimatorTransitionBase>().Any();

        if (needsAnimator && !hasAnimator)
        {
            EditorGUILayout.HelpBox("Please select an AnimatorController to use this mode.", MessageType.Warning);
            return true;
        }

        if (needsTransition && !hasTransition)
        {
            EditorGUILayout.HelpBox("Please select one or more AnimatorStateTransitions to use this mode.", MessageType.Warning);
            return true;
        }

        return false;
    }

    private void BuildParameterTypeMap()
    {
        parameterTypeMap.Clear();

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
            AssetDatabase.GetAssetPath(Selection.activeObject)
        );

        if (controller != null)
        {
            foreach (var p in controller.parameters)
            {
                if (!parameterTypeMap.ContainsKey(p.name))
                    parameterTypeMap[p.name] = p.type;
            }
        }
    }

    private AnimatorController ResolveControllerFromSelection()
    {
        // Case 1: Direct selection
        if (Selection.activeObject is AnimatorController direct)
            return direct;

        // Case 2: Transition or state machine selected
        foreach (var obj in Selection.objects)
        {
            var path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path))
            {
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
                if (controller != null)
                    return controller;
            }
        }

        return null;
    }



    private void RefreshSelection()
    {
        conditionRows.Clear();
        selectedTransitions = Selection.objects.OfType<AnimatorTransitionBase>().ToArray();

        var controller = ResolveControllerFromSelection();

        if (controller == null) return;

        List<AnimatorTransitionBase> transitionsToUse = new();

        switch (bulkMode)
        {
            case BulkSelectionMode.SelectedOnly:
                transitionsToUse.AddRange(selectedTransitions);
                break;

            case BulkSelectionMode.AllLayers:
                transitionsToUse.AddRange(TransitionUtils.GetAllTransitions(controller));
                break;

            case BulkSelectionMode.SpecificLayers:
                transitionsToUse.AddRange(TransitionUtils.GetTransitionsFromLayers(controller, specificLayerNames));
                break;
        }

        for (int i = 0; i < transitionsToUse.Count; i++)
        {
            var t = transitionsToUse[i];
            var conditions = t.conditions;
            for (int j = 0; j < conditions.Length; j++)
            {
                conditionRows.Add(new ConditionRow(t, i, j, conditions[j]));
            }
        }
        conditionRows = TransitionUtils.CollectTransitionConditionsWithSource(
            controller, bulkMode, specificLayerNames, selectedTransitions
        );

        BuildParameterTypeMap();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Condition Splitter", EditorStyles.boldLabel);

        DrawBulkModeSelector();
        if (ShowSelectionWarning()) return;
        DrawGroupingSelector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField($"Grouped Conditions: {conditionRows.Count}", EditorStyles.boldLabel);

        var grouped = ConditionGrouping.GroupRows(conditionRows, selectedGrouping, parameterTypeMap);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));

        foreach (var kvp in grouped)
        {
            DrawGroupedRow(kvp.Key, kvp.Value);
        }
        EditorGUILayout.EndScrollView();


        GUILayout.Space(10);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Apply Changes"))
            {
                foreach (var kvp in grouped)
                {
                    var group = kvp.Value;
                    var first = group[0];

                    foreach (var row in group)
                    {
                        var t = row.transition;
                        var conditions = t.conditions;
                        if (row.conditionIndex < conditions.Length)
                        {
                            Undo.RecordObject(t, "Grouped Condition Edit");

                            var c = conditions[row.conditionIndex];
                            if (!group.Any(r => r.mixedParameter)) c.parameter = first.condition.parameter;
                            if (!group.Any(r => r.mixedMode)) c.mode = first.condition.mode;
                            if (!group.Any(r => r.mixedThreshold)) c.threshold = first.condition.threshold;

                            conditions[row.conditionIndex] = c;
                            t.conditions = conditions;
                            EditorUtility.SetDirty(t);
                        }
                    }
                }

                RefreshSelection();
            }

            if (GUILayout.Button("Refresh"))
            {
                RefreshSelection();
            }
        }
    }

    private void DrawBulkModeSelector()
    {
        EditorGUILayout.LabelField("Bulk Selection Mode", EditorStyles.boldLabel);
        var newMode = (BulkSelectionMode)EditorGUILayout.EnumPopup(bulkMode);

        if (newMode != lastBulkMode)
        {
            lastBulkMode = newMode;
            RefreshSelection(); // ✅ Trigger refresh on mode change
        }


        if (bulkMode == BulkSelectionMode.SpecificLayers)
        {
            EditorGUILayout.LabelField("Include Layers (comma-separated):");
            string input = EditorGUILayout.TextField(string.Join(",", specificLayerNames));
            specificLayerNames = input.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
        }
    }

    private void DrawGroupingSelector()
    {
        EditorGUILayout.LabelField("Group Conditions By", EditorStyles.boldLabel);
        selectedGrouping = (ConditionGroupingType)EditorGUILayout.EnumPopup(selectedGrouping);
    }

    private void DrawGroupedRow(string groupKey, List<ConditionRow> group)
    {
        var first = group[0];
        var mixedParam = group.Any(r => r.condition.parameter != first.condition.parameter);
        var mixedMode = group.Any(r => r.condition.mode != first.condition.mode);
        var mixedThreshold = group.Any(r => !Mathf.Approximately(r.condition.threshold, first.condition.threshold));

        EditorGUILayout.BeginHorizontal("box");
        EditorGUILayout.LabelField($"Group: {groupKey}", GUILayout.Width(100));

        EditorGUI.showMixedValue = mixedParam;
        EditorGUI.BeginChangeCheck();
        string newParam = EditorGUILayout.TextField(first.condition.parameter, GUILayout.Width(120));
        if (EditorGUI.EndChangeCheck())
        {
            foreach (var r in group) r.condition.parameter = newParam;
            foreach (var r in group) r.mixedParameter = false;
        }

        EditorGUI.showMixedValue = mixedMode;
        EditorGUI.BeginChangeCheck();
        var newMode = (AnimatorConditionMode)EditorGUILayout.EnumPopup(first.condition.mode, GUILayout.Width(80));
        if (EditorGUI.EndChangeCheck())
        {
            foreach (var r in group) r.condition.mode = newMode;
            foreach (var r in group) r.mixedMode = false;
        }

        if (newMode is AnimatorConditionMode.Equals or AnimatorConditionMode.NotEqual or AnimatorConditionMode.Greater or AnimatorConditionMode.Less)
        {
            EditorGUI.showMixedValue = mixedThreshold;
            EditorGUI.BeginChangeCheck();
            float newThreshold = EditorGUILayout.FloatField(first.condition.threshold, GUILayout.Width(80));
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var r in group) r.condition.threshold = newThreshold;
                foreach (var r in group) r.mixedThreshold = false;
            }
        }

        EditorGUI.showMixedValue = false;
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField($"{group.Select(r => r.transition).Distinct().Count()}", GUILayout.Width(25));
        EditorGUILayout.EndHorizontal();
    }
}