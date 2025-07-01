using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;
using ParamType = UnityEngine.AnimatorControllerParameterType;

namespace Awaken.TG.Main.Utility.Animations {
    public partial class AnimatorElement : Element<Location>, IRefreshedByAttachment<AnimatorAttachment> {
        public override ushort TypeForSerialization => SavedModels.AnimatorElement;

        [Saved] Dictionary<int, SavedAnimatorParameter> _parameters = new();
        Animator _animator;
        Dictionary<int, SavedAnimatorParameter> _awaitingParameters;

        bool IsAnimatorAvailable => ParentModel.VisibleToPlayer && _animator is { enabled: true, gameObject: { activeInHierarchy: true } };

        public void InitFromAttachment(AnimatorAttachment spec, bool isRestored) { }

        protected override void OnInitialize() {
            ParentModel.OnVisualLoaded(OnVisualLoaded);
        }

        void OnVisualLoaded(Transform transform) {
            _animator = transform.GetComponentInChildren<Animator>(true);
            if (_animator == null) {
                GameObject viewParentGameObject = ParentModel.ViewParent.gameObject;
                Log.Important?.Error($"[Animator Attachment] {viewParentGameObject.HierarchyPath()} has no Animator component", viewParentGameObject);
                Discard();
                return;
            }

            _animator.writeDefaultValuesOnDisable = false;
            SetParametersAfterAnimatorAvailable(_parameters).Forget();
        }

        public void SetParameter(AnimatorControllerParameterType type, in int hash, SavedAnimatorParameter parameter, bool addToDictionary = true) {
            parameter.type = type;
            SetParameter(hash, parameter, addToDictionary);
        }
        
        public void SetParameter(in int hash, in SavedAnimatorParameter parameter, bool addToDictionary = true) {
            if (addToDictionary) {
                _parameters[hash] = parameter;
            }

            if (IsAnimatorAvailable) {
                SetParameterInternal(_animator, hash, parameter);
                return;
            }
            
            if (_awaitingParameters is null or { Count: 0 }) {
                _awaitingParameters ??= new Dictionary<int, SavedAnimatorParameter>();
                _awaitingParameters[hash] = parameter;
                SetParametersAfterAnimatorAvailable(_awaitingParameters, true).Forget();
                return;
            }
            
            _awaitingParameters[hash] = parameter;
        }
        
        async UniTaskVoid SetParametersAfterAnimatorAvailable(Dictionary<int, SavedAnimatorParameter> parameters, bool clearAfterSet = false) {
            if (!await AsyncUtil.WaitUntil(this, () => IsAnimatorAvailable)) {
                return;
            }
            SetParametersFromDictionary(parameters, _animator);
            if (clearAfterSet) {
                parameters.Clear();
            }
        }
        
        static void SetParametersFromDictionary(Dictionary<int, SavedAnimatorParameter> parameters, Animator animator) {
            foreach ((int hash, SavedAnimatorParameter animatorParam) in parameters) {
                AnimatorControllerParameter param = animator.parameters.FirstOrDefault(p => p.nameHash == hash);
                if (param != null) {
                    SetParameterInternal(animator, hash, animatorParam);
                }
            }
        }

        static void SetParameterInternal(Animator animator, int hash, SavedAnimatorParameter parameter) {
            var type = parameter.type;
            if (type == ParamType.Float) {
                animator.SetFloat(hash, parameter.floatValue);
            } else if (type == ParamType.Int) {
                animator.SetInteger(hash, parameter.intValue);
            } else if (type == ParamType.Bool) {
                animator.SetBool(hash, parameter.boolValue);
            } else if (type == ParamType.Trigger) {
                animator.SetTrigger(hash);
            } else {
                throw new ArgumentOutOfRangeException();
            }
        }
    }
}