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


    private AnimatorTransitionBase[] selectedTransitions;
    private List<ConditionRow> conditionRows = new();
    private ConditionGroupingType selectedGrouping = ConditionGroupingType.ComparisonMode;

    [MenuItem("Tools/Condition Splitting Window")]
    public static void ShowWindow()
    {
        GetWindow<TransitionConditionSplitterWindow>("Condition Splitting");
    }

    private void OnFocus() => RefreshSelection();
    private void OnSelectionChange() => RefreshSelection();

    private Dictionary<string, AnimatorControllerParameterType> parameterTypeMap = new();
    private bool ShowSelectionWarning()
    {
        bool needsAnimator = bulkMode is BulkSelectionMode.AllLayers or BulkSelectionMode.SpecificLayers;
        bool needsTransition = bulkMode is BulkSelectionMode.SelectedOnly;

        bool hasAnimator = Selection.activeObject is AnimatorController;
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
    private List<AnimatorTransitionBase> GetAllTransitions(AnimatorController controller)
    {
        var result = new List<AnimatorTransitionBase>();
        foreach (var layer in controller.layers)
        {
            var sm = layer.stateMachine;
            foreach (var state in sm.states)
                result.AddRange(state.state.transitions);
            result.AddRange(sm.anyStateTransitions);
            result.AddRange(sm.entryTransitions);
        }
        return result;
    }

    private List<AnimatorTransitionBase> GetTransitionsFromLayers(AnimatorController controller, List<string> layerNames)
    {
        var result = new List<AnimatorTransitionBase>();
        foreach (var layer in controller.layers)
        {
            if (!layerNames.Contains(layer.name)) continue;
            var sm = layer.stateMachine;
            foreach (var state in sm.states)
                result.AddRange(state.state.transitions);
            result.AddRange(sm.anyStateTransitions);
            result.AddRange(sm.entryTransitions);
        }
        return result;
    }



    private void RefreshSelection()
    {
        conditionRows.Clear();
        selectedTransitions = Selection.objects.OfType<AnimatorTransitionBase>().ToArray();


        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
            AssetDatabase.GetAssetPath(Selection.activeObject)
        );

        if (controller == null) return;



        //currentLayerName = Selection.activeObject.name;

       
        List<AnimatorTransitionBase> transitionsToUse = new();

        switch (bulkMode)
        {
            case BulkSelectionMode.SelectedOnly:
                transitionsToUse.AddRange(selectedTransitions);
                break;

            case BulkSelectionMode.AllLayers:
                transitionsToUse.AddRange(GetAllTransitions(controller));
                break;

            case BulkSelectionMode.SpecificLayers:
                transitionsToUse.AddRange(GetTransitionsFromLayers(controller, specificLayerNames));
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

        BuildParameterTypeMap();
    }
    private void DrawBulkModeSelector()
    {
        EditorGUILayout.LabelField("Bulk Selection Mode", EditorStyles.boldLabel);
        bulkMode = (BulkSelectionMode)EditorGUILayout.EnumPopup(bulkMode);

        if (bulkMode == BulkSelectionMode.SpecificLayers)
        {
            EditorGUILayout.LabelField("Include Layers (comma-separated):");
            string input = EditorGUILayout.TextField(string.Join(",", specificLayerNames));
            specificLayerNames = input.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Condition Splitter", EditorStyles.boldLabel);

        DrawBulkModeSelector();
        if (ShowSelectionWarning()) return;
        DrawGroupingSelector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField($"Grouped Conditions: {conditionRows.Count}", EditorStyles.boldLabel);

        var grouped = GetGroupedRows();
        foreach (var kvp in grouped)
        {
            DrawGroupedRow(kvp.Key, kvp.Value);
        }

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

    private void DrawGroupingSelector()
    {
        EditorGUILayout.LabelField("Group Conditions By", EditorStyles.boldLabel);
        selectedGrouping = (ConditionGroupingType)EditorGUILayout.EnumPopup(selectedGrouping);
    }

    private Dictionary<string, List<ConditionRow>> GetGroupedRows()
    {
        var grouped = conditionRows
            .GroupBy(GetGroupKey)
            .ToDictionary(g => g.Key, g => g.Select(r => new ConditionRow(r)).ToList());

        var sortedKeys = selectedGrouping switch
        {
            ConditionGroupingType.ParameterType => grouped.Keys
    .OrderBy(k => Enum.TryParse(k, out AnimatorControllerParameterType type) ? (int)type : int.MaxValue)
    .ToList(),

            ConditionGroupingType.ParameterName => grouped.Keys.OrderBy(k => k).ToList(),

            ConditionGroupingType.TransitionTitle => grouped.Keys
                .OrderBy(k => string.IsNullOrEmpty(k)) // unnamed last
                .ThenBy(k => k).ToList(),

            ConditionGroupingType.ComparisonMode => grouped.Keys
                .OrderBy(k => Enum.TryParse(k, out AnimatorConditionMode mode) ? (int)mode : int.MaxValue)
                .ToList(),

            ConditionGroupingType.ThresholdValue => grouped.Keys
                .OrderBy(k => float.TryParse(k, out float val) ? val : float.MaxValue)
                .ToList(),

            _ => grouped.Keys.OrderBy(k => k).ToList()
        };

        return sortedKeys.ToDictionary(k => k, k => grouped[k]);
    }


    private string GetGroupKey(ConditionRow row)
    {
        var c = row.condition;
        return selectedGrouping switch
        {
            ConditionGroupingType.ParameterName => c.parameter,
            ConditionGroupingType.ComparisonMode => c.mode.ToString(),
            ConditionGroupingType.ThresholdValue => c.threshold.ToString("F2"),
            ConditionGroupingType.TransitionTitle => row.transition.name,
            ConditionGroupingType.ParameterType => parameterTypeMap.TryGetValue(c.parameter, out var type) ? type.ToString() : "Unknown",
            _ => "Ungrouped"
        };
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
        EditorGUILayout.EndHorizontal();
    }

    private class ConditionRow
    {
        public AnimatorTransitionBase transition;
        public int transitionIndex;
        public int conditionIndex;
        public AnimatorCondition condition;

        public bool mixedParameter;
        public bool mixedMode;
        public bool mixedThreshold;

        public ConditionRow(AnimatorTransitionBase transition, int transitionIndex, int conditionIndex, AnimatorCondition condition)
        {
            this.transition = transition;
            this.transitionIndex = transitionIndex;
            this.conditionIndex = conditionIndex;
            this.condition = condition;
        }

        public ConditionRow(ConditionRow source)
        {
            transition = source.transition;
            transitionIndex = source.transitionIndex;
            conditionIndex = source.conditionIndex;
            condition = new AnimatorCondition
            {
                parameter = source.condition.parameter,
                mode = source.condition.mode,
                threshold = source.condition.threshold
            };
        }
    }
}