using System;
using System.Linq;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Settings.Controllers;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Wyrdnessing;
using Awaken.TG.MVC;
using Awaken.Utility.Animations;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Graphics.DayNightSystem {
    [RequireComponent(typeof(SphereCollider))]
    public class WyrdnightSphereRepeller : StartDependentView<Hero>, IWyrdnightRepellerSource, ILogicReceiver {
        [SerializeField] bool canMove = false;
        [InfoBox("Requires a mesh renderer if you want to visualize the repeller", VisibleIf = "hasVisuals")]
        [SerializeField] bool hasVisuals = false;
        [SerializeField, ShowIf(nameof(hasVisuals))] string materialProperty = "_Transition";
        [SerializeField, ShowIf(nameof(hasVisuals))] AnimationCurve wyrdRepellerAnimation;
        [SerializeField, ShowIf(nameof(hasVisuals))] bool disableRendererWhenZero = true;
        [SerializeField, ShowIf(nameof(hasVisuals))] Renderer[] renderers = Array.Empty<Renderer>();
        [InfoBox("If parent is animated the initial scale of the parent won't effect the radius calculation", VisibleIf = "isParentAnimated")]
        [SerializeField, ShowIf(nameof(hasVisuals))] bool isParentAnimated = false;

        public bool IsFast => true;
        bool IsDisabled => _disabledByILogicReceiver;

        Transform _transform;
        Vector3 _position;
        float _sqRadius;
        
        int _materialPropertyID;
        Material _materialInstance;
        float _wyrdRepellerTime;
        int _targetIndex;
        float _targetTime;
        
        bool _disabledByILogicReceiver;
        TimeDependent _timeDependent;
        
        TimeDependent TargetTimeDependent => _timeDependent is {HasBeenDiscarded: false} ? _timeDependent : null;
        
        protected override void OnInitialize() {
            var sphereCollider = GetComponent<SphereCollider>();
            _transform = transform;
            if (!canMove) {
                _position = _transform.position;
            }

            Vector3 transformScale = isParentAnimated 
                                              ? Vector3.Scale(_transform.parent.parent.lossyScale, _transform.localScale)
                                              : _transform.lossyScale;
            var radius = sphereCollider.radius * math.max(transformScale.x, math.max(transformScale.y, transformScale.z));
            // square radius adjusted to scale of parents
            _sqRadius = math.square(radius);
            World.Services.Get<WyrdnessService>().RegisterRepeller(this);
            
            if (hasVisuals) {
                renderers = renderers.WhereNotUnityNull().ToArray();
                if (renderers.Length == 0) {
                    Log.Important?.Error("WyrdnightSphereRepeller has visuals enabled but no renderers assigned! Disabling visuals.");
                    hasVisuals = false;
                    return;
                }
                
                _materialPropertyID = Shader.PropertyToID(materialProperty);
                _materialInstance = renderers[0].material;
                _materialInstance.SetFloat(_materialPropertyID, wyrdRepellerAnimation.Evaluate(0));

                for (int i = 1; i < renderers.Length; i++) {
                    renderers[i].material = _materialInstance;
                }
                
                _timeDependent = Target.GetOrCreateTimeDependent();
                OnNightChanged(Target.HeroWyrdNight?.Night ?? false);
                Target.ListenTo(HeroWyrdNight.Events.WyrdNightChanged, OnNightChanged, this);
            }
        }

        void OnEnable() {
            if (IsInitialized) {
                World.Services.Get<WyrdnessService>().RegisterRepeller(this);
            }
        }

        void OnDisable() {
            World.Services.TryGet<WyrdnessService>()?.UnregisterRepeller(this);
        }

        void OnUpdate(float deltaTime) {
            if (IsDisabled) return;
            
            if (renderers.All(r => r == null)) {
                Log.Important?.Error("WyrdnightSphereRepeller has visuals enabled but all renderers have been destroyed! Disabling visuals.");
                TargetTimeDependent?.WithoutUpdate(OnUpdate);
                return;
            }

            _wyrdRepellerTime += _targetIndex == 0 ? -deltaTime : deltaTime;

            if (_wyrdRepellerTime > _targetTime) {
                _wyrdRepellerTime = _targetTime;
                TargetTimeDependent?.WithoutUpdate(OnUpdate);
                
            } else if (_wyrdRepellerTime < 0) {
                _wyrdRepellerTime = 0;
                
                if (disableRendererWhenZero) {
                    SetActiveRenderers(false);
                }
                
                TargetTimeDependent?.WithoutUpdate(OnUpdate);
            }
            
            _materialInstance.SetFloat(_materialPropertyID, wyrdRepellerAnimation.Evaluate(_wyrdRepellerTime));
        }

        void OnNightChanged(bool enabled) {
            if (disableRendererWhenZero) {
                SetActiveRenderers(true);
            }
            _targetIndex = enabled ? wyrdRepellerAnimation.keys.Length - 1 : 0;
            _targetTime = wyrdRepellerAnimation.keys[_targetIndex].time;
            TargetTimeDependent?.WithUpdate(OnUpdate);
        }

        protected override IBackgroundTask OnDiscard() {
            TargetTimeDependent?.WithoutUpdate(OnUpdate);
            _timeDependent = null;
            
            return base.OnDiscard();
        }

        protected override void OnDestroy() {
            Object.Destroy(_materialInstance);
            
            TargetTimeDependent?.WithoutUpdate(OnUpdate);
            _timeDependent = null;
            
            World.Services.TryGet<WyrdnessService>()?.UnregisterRepeller(this);
            base.OnDestroy();
        }

        public bool IsPositionInRepeller(Vector3 position) {
            if (IsDisabled) return false;
            if (canMove) {
                _position = _transform.position;
            }
            return (position - _position).sqrMagnitude < _sqRadius;
        }
        
        void SetActiveRenderers(bool state) {
            renderers.ForEach(r => {
                if (r != null) {
                    r.enabled = state;
                }
            });
        }

        public void OnLogicReceiverStateSetup(bool state) => OnLogicReceiverStateChanged(state);
        public void OnLogicReceiverStateChanged(bool state) {
            _disabledByILogicReceiver = !state;
            gameObject.SetActive(state);
        }
    }
}
