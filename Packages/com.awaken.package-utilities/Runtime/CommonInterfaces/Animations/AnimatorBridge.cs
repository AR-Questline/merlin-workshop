using System;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.CommonInterfaces.Animations {
    [RequireComponent(typeof(Animator))]
    public class AnimatorBridge : MonoBehaviour {
        [SerializeField] StateConfig visibility;
        [SerializeField] StateConfig forceAlwaysAnimate;
        [SerializeField] StateConfig forceCullCompletely;

#if ODIN_INSPECTOR
        [ShowInInspector, ReadOnly]
#endif
        Animator _animator;
#if ODIN_INSPECTOR
        [ShowInInspector, ReadOnly]
#endif
        IAnimatorComponent _animatorComponent;

#if ODIN_INSPECTOR
        [ShowInInspector, ReadOnly]
#endif
        bool _isVisible;
#if ODIN_INSPECTOR
        [ShowInInspector, ReadOnly]
#endif
        bool _isForcedToAlwaysAnimate;
#if ODIN_INSPECTOR
        [ShowInInspector, ReadOnly]
#endif
        bool _isForcedToCullCompletely;
        
        readonly HashSet<IAnimatorBridgeStateProvider> _stateProviders = new();
        
        public bool IsVisible => _isVisible;
        public bool IsValid => this && _animator;

        public static AnimatorBridge GetOrAddDefault(Animator animator) {
            var animatorBridge = animator.GetComponent<AnimatorBridge>();
            if (animatorBridge == null) {
                animatorBridge = animator.gameObject.AddComponent<AnimatorBridge>();
#if UNITY_EDITOR
                animatorBridge.hideFlags = HideFlags.DontSaveInEditor & HideFlags.DontSaveInBuild;
#endif
                animatorBridge.visibility = new StateConfig {
                    hasOn = true,
                    onCullingMode = AnimatorCullingMode.AlwaysAnimate,
                    hasOff = true,
                    offCullingMode = AnimatorCullingMode.CullCompletely
                };
                animatorBridge.forceAlwaysAnimate = new StateConfig() {
                    hasOn = true,
                    onCullingMode = AnimatorCullingMode.AlwaysAnimate,
                };
                animatorBridge.forceCullCompletely = new StateConfig() {
                    hasOn = true,
                    onCullingMode = AnimatorCullingMode.CullCompletely,
                };
                animatorBridge.Init();
                animatorBridge.Recalculate();
            }
            return animatorBridge;
        }

        void OnEnable() {
            Init();
            Recalculate();
        }

        public void SetNonUnityVisible(bool visible) {
            _isVisible = visible;
            Recalculate();
        }

        public void RegisterStateProvider(IAnimatorBridgeStateProvider provider) {
            _stateProviders.Add(provider);
            ResolveStateProviders();
        }

        public void UnregisterStateProvider(IAnimatorBridgeStateProvider provider) {
            _stateProviders.Remove(provider);
            ResolveStateProviders();
        }

        void Init() {
            if (!_animator) {
                _animator = GetComponent<Animator>();
                _animatorComponent = GetComponent<IAnimatorComponent>();
            }
        }

        void ResolveStateProviders() {
            _isForcedToCullCompletely = false;
            _isForcedToAlwaysAnimate = false;
            
            foreach (var provider in _stateProviders) {
                if (provider.ForceAnimationCulling) {
                    _isForcedToCullCompletely = true;
                }
                if (provider.AlwaysAnimate) {
                    _isForcedToAlwaysAnimate = true;
                }
            }
            
            Recalculate();
        }
        
        void Recalculate() {
            var culling = AnimatorCullingMode.CullCompletely;
            
            if (forceCullCompletely.TryOverrideCullingMode(_isForcedToCullCompletely, ref culling)) {
                ApplyCalculatedCullingMode(culling);
                return;
            }
            visibility.CalculateCullingMode(_isVisible, ref culling);
            forceAlwaysAnimate.CalculateCullingMode(_isForcedToAlwaysAnimate, ref culling);
            
            ApplyCalculatedCullingMode(culling);
        }
        
        void ApplyCalculatedCullingMode(AnimatorCullingMode culling) {
            _animator.cullingMode = culling;
            if (ReferenceEquals(_animatorComponent, null) == false) {
                _animatorComponent.UpdateCullingMode(culling);
            }
        }

        [Serializable]
#if ODIN_INSPECTOR
        [InlineProperty]
#endif
        struct StateConfig {
#if ODIN_INSPECTOR
            [HorizontalGroup("On value")]
#endif
            public bool hasOn;
#if ODIN_INSPECTOR
            [HorizontalGroup("Off value")]
#endif
            public bool hasOff;
#if ODIN_INSPECTOR
            [HorizontalGroup("On value"), ShowIf("hasOn")]
#endif
            public AnimatorCullingMode onCullingMode;
#if ODIN_INSPECTOR
            [HorizontalGroup("Off value"), ShowIf("hasOff")]
#endif
            public AnimatorCullingMode offCullingMode;

            public void CalculateCullingMode(bool enabled, ref AnimatorCullingMode cullingMode) {
                if (enabled & hasOn) {
                    cullingMode = (AnimatorCullingMode)math.min((int)cullingMode, (int)onCullingMode);
                } else if (!enabled & hasOff) {
                    cullingMode = (AnimatorCullingMode)math.min((int)cullingMode, (int)offCullingMode);
                }
            }

            public bool TryOverrideCullingMode(bool enabled, ref AnimatorCullingMode cullingMode) {
                if (enabled & hasOn) {
                    cullingMode = onCullingMode;
                    return true;
                }

                if (!enabled & hasOff) {
                    cullingMode = offCullingMode;
                    return true;
                }
   
                return false;
            }
        }
    }
}
