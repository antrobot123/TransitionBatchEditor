using UnityEditor.Animations;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
class TransitionUtils
{
    public static string GetLayerNameFromSelection(AnimatorController controller, UnityEngine.Object[] selection)
    {
        // Case 1: selection contains a root AnimatorStateMachine
        foreach (var obj in selection)
        {
            if (obj is AnimatorStateMachine selectedMachine)
            {
                foreach (var layer in controller.layers)
                {
                    if (layer.stateMachine == selectedMachine)
                        return layer.name;
                }
            }

            if (obj is AnimatorTransitionBase transition)
            {
                foreach (var layer in controller.layers)
                {
                    var sm = layer.stateMachine;

                    if (sm.anyStateTransitions.Contains(transition) ||
                        sm.entryTransitions.Contains(transition))
                        return layer.name;

                    foreach (var state in sm.states)
                    {
                        if (state.state.transitions.Contains(transition))
                            return layer.name;
                    }
                }
            }
        }

        return ""; // ❌ No valid layer found
    }
    public static List<AnimatorTransitionBase> GetTransitionsInLayer(AnimatorController controller, string layerName)
    {
        var result = new List<AnimatorTransitionBase>();


        foreach (var layer in controller.layers)
        {
            if (layer.name != layerName) continue;

            var sm = layer.stateMachine;

            // State transitions
            foreach (var state in sm.states)
                result.AddRange(state.state.transitions);

            // AnyState transitions
            result.AddRange(sm.anyStateTransitions);

            // Entry transitions
            result.AddRange(sm.entryTransitions);
        }

        return result;
    }
    public static List<AnimatorTransitionBase> GetAllTransitions(AnimatorController controller)
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
    public static List<AnimatorTransitionBase> GetTransitionsFromLayers(AnimatorController controller, List<string> layerNames)
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
    public static void PrintTransitions(List<AnimatorTransitionBase> transitions, AnimatorController controller, string label = "")
    {
        string temp = "";
        foreach (var t in transitions)
        {
            temp += t.GetDisplayName(controller) + " | ";
        }
        Debug.Log($"{label}: {transitions.Count} transitions: {temp}");
    }
}

