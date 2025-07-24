using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Animations;
using UnityEditor;

public class GroupManager
{
    private List<ConditionRow> conditionRows;
    private ConditionGroupingType selectedGrouping;
    private Dictionary<string, AnimatorControllerParameterType> parameterTypeMap = new();


    public Dictionary<string, List<ConditionRow>> GetGroupedRows()
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


    public string GetGroupKey(ConditionRow row)
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
    public void BuildParameterTypeMap()
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
    public int getGroupCount() { return conditionRows.Count; }
    public void clearRows() { conditionRows.Clear(); }
    public void addToRows(ConditionRow row) { conditionRows.Add(row); }
}

