using System;
using System.Collections;
using System.Collections.Generic;
using Awaken.Utility.Collections;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.Animations;
#endif

namespace Awaken.TG.Debugging.AssetViewer {
    public class DebugAnimationRunner : MonoBehaviour {

        public Animator Animator { get; private set; }
        
        public void Init(Animator animator) {
            Animator = animator;
        }

#if UNITY_EDITOR
        List<AnimatorStateTransition> _animatorStateTransitions;

        void DisableTransitions() {
            _animatorStateTransitions = new List<AnimatorStateTransition>();
            if(Animator.runtimeAnimatorController is AnimatorController ac)
            {
                ac.layers.ForEach(
                    l => l.stateMachine.states.ForEach(
                        s => s.state.transitions.ForEach(DisableTransition)));
            }
        }
        void DisableTransition(AnimatorStateTransition stateTransition) {
            if (!stateTransition.mute) {
                stateTransition.mute = true;
                _animatorStateTransitions.Add(stateTransition);
            }
        }

        void OnDestroy() {
            _animatorStateTransitions?.ForEach(t => { 
                if (t != null) {
                    t.mute = false;
                }
            });
        }
        
        public void PlayAnimation(string stateName, int index) {
            if (_animatorStateTransitions == null) {
                DisableTransitions();
            }
            StopAllCoroutines();
            StartCoroutine(PlayAnimationCoroutine(stateName, index));
        }

        IEnumerator PlayAnimationCoroutine(string stateName, int index) {
            Animator.Play(stateName, index, 0);
            do {
                yield return null;
            } while (!Animator.GetCurrentAnimatorStateInfo(index).IsName(stateName));

            while (IsCurrentStatePlaying(stateName, index)) {
                yield return null;
            }
            
            StartCoroutine(PlayAnimationCoroutine(stateName, index));
        }

        bool IsCurrentStatePlaying(string stateName, int index) {
            var state = Animator.GetCurrentAnimatorStateInfo(index);
            return state.IsName(stateName) && state.normalizedTime < 1;
        }
#endif
    }
}