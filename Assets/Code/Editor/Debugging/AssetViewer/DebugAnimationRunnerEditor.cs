using System;
using Awaken.TG.Debugging.AssetViewer;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Awaken.TG.Editor.Debugging.AssetViewer {
    
    [CustomEditor(typeof(DebugAnimationRunner))]
    public class DebugAnimationRunnerEditor : UnityEditor.Editor {

        DebugAnimationRunner _target;

        void OnEnable() {
            _target = target as DebugAnimationRunner;
        }

        public override void OnInspectorGUI() {
            if (_target.Animator == null || _target.Animator.runtimeAnimatorController is not AnimatorController ac) {
                EditorGUILayout.LabelField("Animator is null or you are not in runtime");
            } else {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(_target.Animator, typeof(Animator), false);
                EditorGUI.EndDisabledGroup();
                DrawAnimator(ac);
            }
        }

        void DrawAnimator(AnimatorController animator) {
            for(int i = 0; i<animator.layers.Length; i++) {
                DrawLayer(animator.layers[i], i);
            }
        }

        void DrawLayer(AnimatorControllerLayer controllerLayer, int layerIndex) {
            GUILayout.BeginVertical(controllerLayer.name, "window");
            DrawChildren(controllerLayer.stateMachine, layerIndex);
            GUILayout.EndVertical();
        }

        void DrawChildren(AnimatorStateMachine animatorStateMachine, int layerIndex) {
            foreach (ChildAnimatorState childAnimatorState in animatorStateMachine.states) {
                DrawButtons(childAnimatorState, layerIndex);
            }

            foreach (ChildAnimatorStateMachine childAnimatorStateMachine in animatorStateMachine.stateMachines) {
                DrawChildren(childAnimatorStateMachine.stateMachine, layerIndex);
            }
        }

        void DrawButtons(ChildAnimatorState childAnimatorState, int layerIndex) {
            if (GUILayout.Button(childAnimatorState.state.name)) {
                _target.PlayAnimation(childAnimatorState.state.name, layerIndex);
            }
        }
    }
}