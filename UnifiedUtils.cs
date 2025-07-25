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
    public static bool TryGetUnifiedEnum<TEnum>(
    AnimatorTransitionBase[] transitions,
    Func<AnimatorStateTransition, TEnum> selector,
    out TEnum unifiedValue
) where TEnum : struct, Enum
    {
        unifiedValue = default;
        bool hasReference = false;

        foreach (var t in transitions)
        {
            if (t is AnimatorStateTransition stateTransition)
            {
                TEnum value = selector(stateTransition);
                if (!hasReference)
                {
                    unifiedValue = value;
                    hasReference = true;
                }
                else if (!EqualityComparer<TEnum>.Default.Equals(unifiedValue, value))
                {
                    return false;
                }
            }
        }

        return hasReference;
    }
    public static TEnum DrawUnifiedEnumPopup<TEnum>(
    string label,
    AnimatorTransitionBase[] transitions,
    Func<AnimatorStateTransition, TEnum> selector,
    ref TEnum cachedValue
) where TEnum : struct, Enum
    {
        bool isUnified = TryGetUnifiedEnum(transitions, selector, out TEnum liveValue);
        if (isUnified) cachedValue = liveValue;

        EditorGUI.showMixedValue = !isUnified;
        TEnum newValue = (TEnum)EditorGUILayout.EnumPopup(label, cachedValue);
        EditorGUI.showMixedValue = false;

        return newValue;
    }
}