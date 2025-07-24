using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;

public enum ConditionGroupingType
{
    ParameterName,
    ComparisonMode,
    ThresholdValue,
    TransitionTitle,
    ParameterType
}

public static class ConditionGrouping
{
    public static Dictionary<string, List<ConditionRow>> GroupRows(
        List<ConditionRow> rows,
        ConditionGroupingType groupingType,
        Dictionary<string, AnimatorControllerParameterType> parameterTypeMap)
    {
        var grouped = rows
            .GroupBy(row => GetGroupKey(row, groupingType, parameterTypeMap))
            .ToDictionary(g => g.Key, g => g.Select(r => new ConditionRow(r)).ToList());

        List<string> sortedKeys = groupingType switch
        {
            ConditionGroupingType.ParameterType => grouped.Keys
                .OrderBy(k => Enum.TryParse(k, out AnimatorControllerParameterType type) ? (int)type : int.MaxValue)
                .ToList(),

            ConditionGroupingType.ParameterName => grouped.Keys.OrderBy(k => k).ToList(),

            ConditionGroupingType.TransitionTitle => grouped.Keys
                .OrderBy(k => string.IsNullOrEmpty(k))
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

    public static string GetGroupKey(
        ConditionRow row,
        ConditionGroupingType groupingType,
        Dictionary<string, AnimatorControllerParameterType> parameterTypeMap)
    {
        var c = row.condition;
        return groupingType switch
        {
            ConditionGroupingType.ParameterName => c.parameter,
            ConditionGroupingType.ComparisonMode => c.mode.ToString(),
            ConditionGroupingType.ThresholdValue => c.threshold.ToString("F2"),
            ConditionGroupingType.TransitionTitle => row.transition.name,
            ConditionGroupingType.ParameterType =>
                parameterTypeMap.TryGetValue(c.parameter, out var type) ? type.ToString() : "Unknown",
            _ => "Ungrouped"
        };
    }
}