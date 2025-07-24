using UnityEditor;
using UnityEngine;
using UnityEditor.Animations;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class TransitionBatchEditor : EditorWindow
{
    private AnimatorStateTransition[] selectedTransitions;
    private TransitionEditState editState;

    [MenuItem("Tools/Transition Batch Editor")]
    public static void ShowWindow()
    {
        GetWindow<TransitionBatchEditor>("Transition Batch Editor");
    }

    private void OnFocus() => RefreshSelection();
    private void OnSelectionChange()
    {
        RefreshSelection();
        Repaint();
    }

    private void RefreshSelection()
    {
        selectedTransitions = Selection.objects
            .OfType<AnimatorStateTransition>()
            .ToArray();

        editState = TransitionEditState.FromSelection(selectedTransitions);
    }

    private void OnGUI()
    {
        if (selectedTransitions == null || selectedTransitions.Length == 0)
        {
            EditorGUILayout.HelpBox("Select AnimatorStateTransitions to begin editing.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField($"Selected Transitions: {selectedTransitions.Length}", EditorStyles.boldLabel);

        // 🧠 Generic input rendering
        RenderToggle("Has Exit Time", ref editState.hasExitTime, ref editState.mixedHasExitTime);
        RenderFloat("Exit Time", ref editState.exitTime, ref editState.mixedExitTime);

        RenderToggle("Fixed Duration", ref editState.hasFixedDuration, ref editState.mixedHasFixedDuration);
        RenderFloat("Duration", ref editState.duration, ref editState.mixedDuration);

        RenderFloat("Offset", ref editState.offset, ref editState.mixedOffset);

        GUILayout.Space(10);

        if (GUILayout.Button("Apply to Selected Transitions"))
        {
            foreach (var t in selectedTransitions)
            {
                Undo.RecordObject(t, "Batch Transition Edit");
                editState.ApplyTo(t);
                EditorUtility.SetDirty(t);
            }

            RefreshSelection(); // Resync after apply
        }

        if (GUILayout.Button("Refresh"))
        {
            RefreshSelection(); // Manual sync
        }
    }

    private void RenderFloat(string label, ref float value, ref bool mixedFlag)
    {
        EditorGUI.showMixedValue = mixedFlag;
        EditorGUI.BeginChangeCheck();
        float newValue = EditorGUILayout.FloatField(label, value);
        if (EditorGUI.EndChangeCheck())
        {
            value = newValue;
            mixedFlag = false;
        }
        EditorGUI.showMixedValue = false;
    }

    private void RenderToggle(string label, ref bool value, ref bool mixedFlag)
    {
        EditorGUI.showMixedValue = mixedFlag;
        EditorGUI.BeginChangeCheck();
        bool newValue = EditorGUILayout.Toggle(label, value);
        if (EditorGUI.EndChangeCheck())
        {
            value = newValue;
            mixedFlag = false;
        }
        EditorGUI.showMixedValue = false;
    }
}