using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Graphics.Animations;
using Awaken.TG.Main.General;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor.Animations;
#endif
using UnityEngine;

namespace Awaken.TG.Main.Fights.FPP {
    public class AnimatorParameter : StateMachineBehaviour {
#if UNITY_EDITOR
        [ValueDropdown(nameof(Parameters))]
        [OnValueChanged(nameof(SetValueType))]
#endif
        public string parameterName;
        [ShowIf(nameof(ShowFloat))]
        public float floatValue;
        [ShowIf(nameof(ShowBool))]
        public bool boolValue;
        [ShowIf(nameof(ShowInt))]
        public int intValue;
        [ShowIf(nameof(ShowTriggerType))]
        public bool resetTrigger = false;
        [ShowIf(nameof(ShowSetOverTime)), HideIf(nameof(ShowClamp))]
        [Tooltip("Final value will be set when the animation is over")]
        public bool setOverTime;
        [HideIf(nameof(setOverTime)), HideIf(nameof(ShowClamp))]
        public UpdateType updateType;
        [ShowIf(nameof(ShowDelayBool)), HideIf(nameof(ShowClamp))] 
        public bool withDelay;
        [ShowIf(nameof(withDelay)), Range(0,1)]
        public float delayValue;
        public bool clampValue;
        [ShowIf(nameof(ShowClamp))]
        public FloatRange clampedValue = new FloatRange(0, 1);
        [HideInInspector]
        public ValueType valueToSet;
        bool ShowFloat => valueToSet == ValueType.Float && !ShowClamp;
        bool ShowBool => valueToSet == ValueType.Bool;
        bool ShowInt => valueToSet == ValueType.Integer && !ShowClamp;
        bool ShowTriggerType => valueToSet == ValueType.Trigger;
        bool ShowDelayBool => updateType == UpdateType.OnUpdate && !setOverTime;
        bool ShowSetOverTime => ShowInt || ShowFloat;
        bool ShowClamp => (valueToSet is ValueType.Float or ValueType.Integer) && clampValue;

#if UNITY_EDITOR
        [HideInInspector]
        public AnimatorController controller;
        List<string> Parameters() {
            List<string> parameters = new List<string>();
            if (controller != null) {
                parameters.AddRange(controller.parameters.Select(parameter => parameter.name));
            }
            return parameters;
        }

        void SetValueType() {
            if (!string.IsNullOrWhiteSpace(parameterName)) {
                var param = controller.parameters.First(p => p.name.Equals(parameterName));
                switch (param.type) {
                    case AnimatorControllerParameterType.Bool:
                        valueToSet = ValueType.Bool;
                        break;
                    case AnimatorControllerParameterType.Float:
                        valueToSet = ValueType.Float;
                        break;
                    case AnimatorControllerParameterType.Int:
                        valueToSet = ValueType.Integer;
                        break;
                    case AnimatorControllerParameterType.Trigger:
                        valueToSet = ValueType.Trigger;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
#endif
        bool _onUpdateValueSet;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            if (animator.GetLayerWeight(layerIndex) < 1 || clampValue) {
                return;
            }
            _onUpdateValueSet = false;
            if (updateType == UpdateType.OnEnter) {
                SetAnimatorValue(animator, stateInfo.normalizedTime);
            }
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            if (animator.GetLayerWeight(layerIndex) < 1) {
                return;
            }

            if (clampValue) {
                SetAnimatorValue(animator, stateInfo.normalizedTime);
            } else if (withDelay) {
                if (stateInfo.normalizedTime >= delayValue && !_onUpdateValueSet) {
                    SetAnimatorValue(animator, stateInfo.normalizedTime);
                    _onUpdateValueSet = true;
                }
            } else if (updateType == UpdateType.OnUpdate || setOverTime) {
                if (setOverTime && animator.IsInTransition(layerIndex)) {
                    return;
                }
                SetAnimatorValue(animator, stateInfo.normalizedTime);
            }
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            if (animator.GetLayerWeight(layerIndex) < 1) {
                return;
            }

            bool isUpdateWithNoValueSet = updateType == UpdateType.OnUpdate && !_onUpdateValueSet && !setOverTime;
            if (updateType == UpdateType.OnExit || isUpdateWithNoValueSet) {
                SetAnimatorValue(animator, stateInfo.normalizedTime);
            }
            _onUpdateValueSet = false;
        }

        void SetAnimatorValue(Animator animator, float normalizedTime) {
            if (normalizedTime >= 1) {
                normalizedTime = 1;
            }
            switch (valueToSet) {
                case ValueType.Float:
                    if (clampValue) {
                        float value = animator.GetFloat(parameterName);
                        value = Mathf.Clamp(value, clampedValue.min, clampedValue.max);
                        animator.SetFloat(parameterName, value);
                    } else if (setOverTime) {
                        animator.SetFloat(parameterName, floatValue * normalizedTime);
                    } else {
                        animator.SetFloat(parameterName, floatValue);
                    }
                    break;
                case ValueType.Bool:
                    animator.SetBool(parameterName, boolValue);
                    break;
                case ValueType.Trigger:
                    if (resetTrigger) {
                        animator.ResetTrigger(parameterName);
                    } else {
                        animator.SetTrigger(parameterName);
                    }
                    break;
                case ValueType.Integer:
                    if (clampValue) {
                        int value = animator.GetInteger(parameterName);
                        value = Math.Clamp(value, Mathf.FloorToInt(clampedValue.min), Mathf.CeilToInt(clampedValue.max));
                        animator.SetInteger(parameterName, value);
                    } else if (setOverTime) {
                        animator.SetInteger(parameterName, (int)(intValue * normalizedTime));    
                    } else {
                        animator.SetInteger(parameterName, intValue);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public enum ValueType {
            Float = 0,
            Bool = 1,
            Trigger = 2,
            Integer = 4,
        }

        public enum UpdateType {
            OnEnter = 0,
            OnUpdate = 1,
            OnExit = 2,
        }
    }
}