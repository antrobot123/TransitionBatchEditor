using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class UnifiedUtils
{
    public static bool TryGetUnifiedFloat(
        AnimatorTransitionBase[] transitions,
        System.Func<AnimatorStateTransition, float> selector,
        out float unifiedValue
    )
    {
        unifiedValue = 0f;
        bool hasReference = false;

        foreach (var t in transitions)
        {
            if (t is AnimatorStateTransition stateTransition)
            {
                float value = selector(stateTransition);
                if (!hasReference)
                {
                    unifiedValue = value;
                    hasReference = true;
                }
                else if (!Mathf.Approximately(unifiedValue, value))
                {
                    return false;
                }
            }
        }

        return hasReference;
    }

    public static bool TryGetUnifiedBool(
        AnimatorTransitionBase[] transitions,
        System.Func<AnimatorStateTransition, bool> selector,
        out bool unifiedValue
    )
    {
        unifiedValue = false;
        bool hasReference = false;

        foreach (var t in transitions)
        {
            if (t is AnimatorStateTransition stateTransition)
            {
                bool value = selector(stateTransition);
                if (!hasReference)
                {
                    unifiedValue = value;
                    hasReference = true;
                }
                else if (unifiedValue != value)
                {
                    return false;
                }
            }
        }

        return hasReference;
    }

    public static float DrawUnifiedFloatField(
        string label,
        AnimatorTransitionBase[] transitions,
        System.Func<AnimatorStateTransition, float> selector,
        ref float cachedValue
    )
    {
        bool isUnified = TryGetUnifiedFloat(transitions, selector, out float liveValue);
        if (isUnified) cachedValue = liveValue;

        EditorGUI.showMixedValue = !isUnified;
        float newValue = EditorGUILayout.FloatField(label, cachedValue);
        EditorGUI.showMixedValue = false;
        return newValue;
    }

    public static bool DrawUnifiedBoolField(
        string label,
        AnimatorTransitionBase[] transitions,
        System.Func<AnimatorStateTransition, bool> selector,
        ref bool cachedValue
    )
    {
        bool isUnified = TryGetUnifiedBool(transitions, selector, out bool liveValue);
        if (isUnified) cachedValue = liveValue;

        EditorGUI.showMixedValue = !isUnified;
        bool newValue = EditorGUILayout.Toggle(label, cachedValue);
        EditorGUI.showMixedValue = false;
        return newValue;
    }
}