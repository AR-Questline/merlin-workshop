using System;
using Awaken.TG.Main.General;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using DG.Tweening;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Graphics.VFX.ShaderControlling {
    public class AdvancedShaderController : MonoBehaviour {
        [SerializeField, ListDrawerSettings(DefaultExpandedState = true)] EntryData[] entries = Array.Empty<EntryData>();
        
        [SerializeField] float delay;
        [SerializeField] float duration;
        [SerializeField] OnEndType onEnd;
        [SerializeField] bool unscaledTime;
        [SerializeField] bool tweenOnStart;
        [SerializeField] bool tweenOnEnable;

        [Header("Debug")]
        [ShowInInspector, ReadOnly, HideInEditorMode] State _state;
        [ShowInInspector, ReadOnly, HideInEditorMode] float _delayTimer;
        [ShowInInspector, ReadOnly, HideInEditorMode] float _percent;
        
        float DeltaTime => unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        public float Duration {
            [UnityEngine.Scripting.Preserve] get => duration;
            set => duration = value;
        }
        
        void Awake() {
            foreach (ref var entry in entries.RefIterator()) {
                entry.Init();
            }
        }

        void Start() {
            if (tweenOnStart) {
                StartEffect(true);
            }
        }

        void OnEnable() {
            if (tweenOnEnable) {
                StartEffect(true);
            }
        }

        void OnDisable() {
            if (_state != State.None) {
                StopEffect();
            }
        }

        public void StartEffect(bool forward) {
            if (_state == State.None) {
                World.Services.Get<UnityUpdateProvider>().RegisterAdvancedShaderController(this);
                foreach (ref readonly var entry in entries.RefIterator()) {
                    entry.OnStart();
                }
            }
            _state = forward ? State.DelayBeforeForward : State.DelayBeforeBackward;
            _delayTimer = 0;
            _percent = 0;
            UpdatePercent();
        }

        public void UnityUpdate() {
            if (_state == State.None || _state == State.Keep) {
                return;
            }
            if (_state is State.DelayBeforeForward or State.DelayBeforeBackward) {
                _delayTimer += DeltaTime;
                if (_delayTimer >= delay) {
                    _state = _state is State.DelayBeforeForward ? State.Forward : State.Backward;
                }
                return;
            }
            
            _percent += DeltaTime / duration;
            if (_percent < 1) {
                UpdatePercent();
            } else {
                _percent = 1;
                UpdatePercent();
                
                if (onEnd == OnEndType.Keep) {
                    _state = State.Keep;
                    return;
                }
                
                _percent = 0;

                if (_state == State.Forward) {
                    if (onEnd == OnEndType.PingPong) {
                        _state = State.Backward;
                    } else if (onEnd == OnEndType.Stop) {
                        StopEffect();
                    }
                } else if (_state == State.Backward) {
                    if (onEnd == OnEndType.PingPong) {
                        _state = State.Forward;
                    } else if (onEnd == OnEndType.Stop) {
                        StopEffect();
                    }
                }
            }
        }

        void UpdatePercent() {
            bool forward = _state is State.DelayBeforeForward or State.Forward;
            foreach (ref readonly var entry in entries.RefIterator()) {
                entry.Update(_percent, forward);
            }
        }

        void StopEffect() {
            foreach (ref readonly var entry in entries.RefIterator()) {
                entry.OnEnd();
            }
            _state = State.None;
            World.Services.Get<UnityUpdateProvider>().UnregisterAdvancedShaderController(this);
        }

        [Serializable]
        struct EntryData {
            public MaterialGatherer.Handle gatherer;
            [ListDrawerSettings(DefaultExpandedState = true)] public PropertyData[] properties;

            public void Init() {
                foreach (ref var property in properties.RefIterator()) {
                    property.Init();
                }
            }

            public readonly void OnStart() {
                gatherer.gatherer.Gather();
            }

            public readonly void OnEnd() {
                gatherer.gatherer.Release();
            }

            public readonly void Update(float percent, bool forward) {
                foreach (ref readonly var property in properties.RefIterator()) {
                    property.Update(percent, gatherer.gatherer.Materials, forward);
                }
            }
        }
        
        [Serializable]
        struct PropertyData {
            public string property;
            public FloatRange value;
            public Easing easing;
            
            int _propertyId;

            public void Init() {
                _propertyId = Shader.PropertyToID(property);
            }
            
            public readonly void Update(float percent, Material[] materials, bool forward) {
                float start = forward ? value.min : value.max;
                float end = forward ? value.max : value.min;
                float interpolatedValue = easing.Interpolate(start, end, percent);
                foreach (var material in materials) {
                    material.SetFloat(_propertyId, interpolatedValue);
                }
            }
        }

        [Serializable, InlineProperty]
        struct Easing {
            [HorizontalGroup, HideLabel] public EasingType easing;
            [HorizontalGroup, HideLabel, ShowIf(nameof(IsEasingEase))] public Ease ease;
            [HorizontalGroup, HideLabel, ShowIf(nameof(IsEasingCurve))] public AnimationCurve curve;
            
            bool IsEasingEase => easing == EasingType.Ease;
            bool IsEasingCurve => easing == EasingType.Curve;

            public readonly float Interpolate(float start, float end, float percent) {
                return easing switch {
                    EasingType.Ease => DOVirtual.EasedValue(start, end, percent, ease),
                    EasingType.Curve => DOVirtual.EasedValue(start, end, percent, curve),
                    _ => math.lerp(start, end, percent),
                };
            }
        }

        enum EasingType : byte {
            [UnityEngine.Scripting.Preserve] None = 0,
            Ease = 1,
            Curve = 2,
        }
        
        enum OnEndType : byte {
            [UnityEngine.Scripting.Preserve] Stop,
            [UnityEngine.Scripting.Preserve] Loop,
            PingPong,
            Keep,
        }

        enum State : byte {
            None,
            DelayBeforeForward,
            DelayBeforeBackward,
            Forward,
            Backward,
            Keep,
        }
    }
}