using System.Threading;
using Animancer;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Shared;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Maths;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Heroes.Base {
    public partial class SynchronizedHeroSubstateMachine : Element<HeroAnimatorSubstateMachine> {
        public sealed override bool IsNotSaved => true;

        HeroStateType _currentStateType = HeroStateType.Empty;
        AnimancerState _currentState;
        AvatarMask _overridenAvatarMask;
        AvatarMask _avatarMask;
        AvatarMask _tppAvatarMask;
        
        MixerState<Vector2> _mixerState;
        MixerState<Vector2> _parentMixerState;
        
        bool _isActive;
        bool _isCurrentStateHeavyAttack;
        float _desiredWeight;
        float? _overridenDesiredWeight;
        float? _transitionSpeed;
        readonly bool _removeEvents;
        readonly bool _isHeadLayer;
        readonly bool _isActiveLayer;
        readonly bool _isAdditive;

        CancellationTokenSource _animationLoadToken;
        bool _isLoadingAnimation;

        public HeroLayerType LayerType { get; }
        public  HeroLayerType? LayerToSynchronize { get; }
        public AnimancerLayer AnimancerLayer { get; private set; }
        public AvatarMask AvatarMask => _overridenAvatarMask != null 
            ? _overridenAvatarMask 
            : Hero.TppActive 
                ? _tppAvatarMask 
                : _avatarMask;
        public bool AnyStatePlaying => _currentState != null;
        ARHeroAnimancer HeroAnimancer => ParentModel.HeroAnimancer;
        float DesiredWeight => _overridenDesiredWeight ?? _desiredWeight;

        public SynchronizedHeroSubstateMachine(HeroLayerType layerType, bool removeEvents = true, bool isAdditive = false, AvatarMask overridenAvatarMask = null, HeroLayerType? layerToSynchronize = null) {
            LayerType = layerType;
            LayerToSynchronize = layerToSynchronize;
            _isHeadLayer = IsHeadLayer(layerType);
            _isActiveLayer = IsActiveLayer(layerType);
            _removeEvents = removeEvents;
            _isAdditive = isAdditive;
            _overridenAvatarMask = overridenAvatarMask;
        }

        protected override void OnInitialize() {
            // --- Create animancer layer
            AnimancerLayer = HeroAnimancer.Layers[(int)LayerType];
            if (_overridenAvatarMask == null) {
                _avatarMask = CommonReferences.Get.GetMask(LayerType);
                _tppAvatarMask = CommonReferences.Get.GetTppMask(LayerType);
            }

            AnimancerLayer.SetMask(AvatarMask);
            AnimancerLayer.SetDebugName(LayerType.ToString());
            AnimancerLayer.IsAdditive = _isAdditive;
            
            ParentModel.ParentModel.GetOrCreateTimeDependent().WithUpdate(OnUpdate);
        }

        public void ChangeHeroPerspective(bool tppActive) {
            AnimancerLayer.SetMask(AvatarMask);
        }

        void OnUpdate(float deltaTime) {
            UpdateLayerWeight(deltaTime);
            
            if (!_isActive) {
                return;
            }

            if (ParentModel.CurrentAnimatorState == null) {
                return;
            }
            
            UpdateMixerState();
            UpdateStateSpeed();

            if (_isLoadingAnimation) {
                return;
            }

            var parentState = ParentModel.CurrentAnimatorState.CurrentState;
            if (parentState != null && _currentState != null && (_currentState.Clip != null || _mixerState != null)) {
                float synchronizeNormalizedTime = AnimancerUtils.SynchronizeNormalizedTime(parentState, deltaTime);
                _currentState.SetNormalizedTimeWithEventsInvoke(synchronizeNormalizedTime);
            }
        }

        void UpdateLayerWeight(float deltaTime) {
            if (_transitionSpeed != null) {
                float currentWeight = AnimancerLayer.Weight;
                currentWeight = mathExt.MoveTowards(currentWeight, DesiredWeight, deltaTime * _transitionSpeed.Value);
                if (math.abs(currentWeight - DesiredWeight) < 0.01f) {
                    SetLayerWeight(DesiredWeight);
                    _transitionSpeed = null;
                } else {
                    SetLayerWeight(currentWeight);
                }
            }
        }

        void SetLayerWeight(float weight) {
            AnimancerLayer.Weight = weight;
            if (weight <= 0 && _desiredWeight <= 0 && _overridenDesiredWeight is null or <= 0) {
                AnimancerLayer.Stop();
                AnimancerLayer.DestroyStates();
            }
        }

        void UpdateMixerState() {
            if (_mixerState is not { IsValid: true }) {
                return;
            }

            if (_parentMixerState is not { IsValid: true } && !TrySetParentLinearMixerState()) {
                return;
            }

            _mixerState.Parameter = _parentMixerState!.Parameter;
        }

        void UpdateStateSpeed() {
            if (_currentState != null && ParentModel.CurrentAnimatorState is IStateWithModifierAttackSpeed attackSpeedModifier) {
                _currentState.Speed = attackSpeedModifier.AttackSpeed;
            }
        }

        public void SetEnable(bool isActive, float? weight = null, float? transitionSpeed = null) {
            if (_isActive != isActive) {
                _isActive = isActive;
                if (!_isActive) {
                    SetDesiredLayerWeight(0, transitionSpeed);
                } else {
                    _currentStateType = HeroStateType.Empty;
                    
                    if (weight.HasValue) {
                        SetDesiredLayerWeight(weight.Value, transitionSpeed);
                    }
                    
                    SetCurrentState(ParentModel.CurrentStateToEnterType, 0f);
                }
            }

            if (_isActive) {
                if (_isHeadLayer) {
                    _overridenDesiredWeight = ParentModel.TryGetStateOfType(_currentStateType)?.HeadLayerWeightOverride;
                }
                
                if (weight.HasValue) {
                    SetDesiredLayerWeight(weight.Value, transitionSpeed);
                }
                
                AnimancerLayer.IsAdditive = _isAdditive;
            }
        }

        void SetDesiredLayerWeight(float desiredWeight, float? transitionSpeed) {
            _desiredWeight = desiredWeight;
            _transitionSpeed = transitionSpeed;
            if (transitionSpeed == null || _transitionSpeed.Value <= 0) {
                SetLayerWeight(DesiredWeight);
            }
        }

        public void SetCurrentState(HeroStateType stateToEnter, float? overrideCrossFadeTime) {
            if (!_isActive) {
                return;
            }
            
            if (_currentStateType == stateToEnter) {
                return;
            }
            
            _animationLoadToken?.Cancel();
            _animationLoadToken = new CancellationTokenSource();
            
            _currentStateType = stateToEnter;
            if (_isHeadLayer) {
                _overridenDesiredWeight = ParentModel.TryGetStateOfType(_currentStateType)?.HeadLayerWeightOverride;
                SetLayerWeight(DesiredWeight);
            }
            
            if (DesiredWeight == 0) {
                return;
            }
            
            _isLoadingAnimation = true;
            
            // --- Head and Active animations are the same as main layer but use separate mask
            var layerType = LayerToSynchronize ?? ( _isHeadLayer || _isActiveLayer ? ParentModel.LayerType : LayerType);
            HeroAnimancer.GetAnimancerNode(layerType, stateToEnter,
                n => OnNodeLoaded(n, overrideCrossFadeTime, _animationLoadToken).Forget(),
                OnFailedFindNode, _animationLoadToken.Token);
        }
        
        async UniTaskVoid OnNodeLoaded(ITransition node, float? overrideCrossFadeTime, CancellationTokenSource token) {
            if (!await AsyncUtil.WaitWhile(ParentModel, () => ParentModel.CurrentAnimatorState is { CurrentState: null }, token)) {
                return;
            }

            if (ParentModel == null || ParentModel.HasBeenDiscarded || ParentModel.CurrentAnimatorState == null ||
                ParentModel.CurrentStateToEnterType != _currentStateType) {
                return;
            }

            float fadeDuration = overrideCrossFadeTime ?? ParentModel.CurrentAnimatorState.EntryTransitionDuration;
            bool isMixer = node is MixerTransition2D;
            _currentState = AnimancerLayer.Play(node, fadeDuration, isMixer ? default : FadeMode.FromStart);
            if (_currentState.HasEvents && _removeEvents) {
                _currentState.Events = new AnimancerEvent.Sequence();
                _currentState.Events.Clear();
            }
            _currentState.SetNormalizedTimeWithEventsInvoke(ParentModel.CurrentAnimatorState.CurrentState.NormalizedTime);
            _mixerState = _currentState as MixerState<Vector2>;
            HeroAnimancer.RebindAnimationRigging();
            TrySetParentLinearMixerState();
            _isLoadingAnimation = false;
        }
        
        void OnFailedFindNode() {
            if (ParentModel == null || ParentModel.HasBeenDiscarded || ParentModel.CurrentStateToEnterType != _currentStateType) {
                return;
            }
            _isLoadingAnimation = false;
            SetCurrentState(ParentModel.DefaultState, 0f);
        }

        bool TrySetParentLinearMixerState() {
            _parentMixerState = ParentModel.CurrentAnimatorState.CurrentState as MixerState<Vector2>;
            return _parentMixerState != null;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            ParentModel.ParentModel.GetTimeDependent()?.WithoutUpdate(OnUpdate);
        }
        
        // === Helpers
        static bool IsHeadLayer(HeroLayerType heroLayerType) {
            return heroLayerType is HeroLayerType.HeadMainHand or HeroLayerType.HeadOffHand
                or HeroLayerType.HeadBothHands or HeroLayerType.HeadTools or HeroLayerType.HeadFishing
                or HeroLayerType.HeadSpyglass or HeroLayerType.HeadOverrides;
        }

        static bool IsActiveLayer(HeroLayerType heroLayerType) {
            return heroLayerType is HeroLayerType.ActiveMainHand or HeroLayerType.ActiveOffHand;
        }
    }
}