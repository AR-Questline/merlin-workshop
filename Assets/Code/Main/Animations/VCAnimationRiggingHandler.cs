using System.Linq;
using Animancer;
using Awaken.TG.Main.AI.Idle.Interactions;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Awaken.TG.Main.AI.Idle.Behaviours;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.NPCs.Providers;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using UniversalProfiling;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Animations {
    public partial class VCAnimationRiggingHandler : ViewComponent<Location>, ICanMoveProvider {
        static readonly UniversalProfilerMarker RebindAnimatorMarker = new (Color.yellow, $"{nameof(VCAnimationRiggingHandler)}.Animator.Rebind");
        
        [SerializeField] Transform lookAt;
        [SerializeField] float lookAtTargetSpeed = 5f;
        [SerializeField, FoldoutGroup("Rigs")] Rig rootRig;
        [SerializeField, FoldoutGroup("Rigs")] Rig headRig;
        [SerializeField, FoldoutGroup("Rigs")] Rig bodyRig;
        [SerializeField, FoldoutGroup("Rigs")] Rig combatRig;
        [SerializeField, FoldoutGroup("Rigs")] Rig attackRig;
        [SerializeField, FoldoutGroup("Rig Update Settings")] AnimationCurve rigWeightCurve;
        [SerializeField, FoldoutGroup("Rig Update Settings")] float defaultHeadRigUpdateSpeed = 0.7f;
        [SerializeField, FoldoutGroup("Rig Update Settings")] float defaultBodyRigUpdateSpeed = 0.7f;
        [SerializeField, FoldoutGroup("Rig Update Settings")] float defaultRootRigUpdateSpeed = 0.4f;
        [SerializeField, FoldoutGroup("Rig Update Settings")] float defaultCombatRigUpdateSpeed = 2.5f;
        [SerializeField, FoldoutGroup("Rig Update Settings")] float defaultUpDownTurnSpeed = 1.5f;
        //Combat
        [SerializeField, FoldoutGroup("Combat/IK Targets")] Transform combatIKPosUp;
        [SerializeField, FoldoutGroup("Combat/IK Targets")] Transform combatIKPosLow;
        [SerializeField, FoldoutGroup("Combat/IK Targets")] Transform combatIKTarget;
        [SerializeField, Range(0f, 1f), FoldoutGroup("Combat")] float combatIKSlider;
        [SerializeField, Range(-3, -1f), FoldoutGroup("Combat")] float reachMaxLookDownWhenHeroBelow = -2f;
        [SerializeField, Range(1f, 3f), FoldoutGroup("Combat")] float reachMaxLookUpWhenHeroAbove = 2f;
        [SerializeField, FoldoutGroup("Combat")] float ignoreHeightDifferenceThreshold = 0.5f;
        // Glance
        [SerializeField, FoldoutGroup("Glancing")] FloatRange glanceTimeRange = new FloatRange(3.7f, 5.9f);
        [SerializeField, FoldoutGroup("Glancing")] FloatRange glanceDelayRange = new FloatRange(1.8f, 3.8f);
        // Preview
        [ShowInInspector, ReadOnly, Range(0f, 1f), FoldoutGroup("Preview")] float _rootRigWeightPreview;
        [ShowInInspector, ReadOnly, Range(0f, 1f), FoldoutGroup("Preview")] float _headRigWeightPreview;
        [ShowInInspector, ReadOnly, Range(0f, 1f), FoldoutGroup("Preview")] float _bodyRigWeightPreview;
        [ShowInInspector, ReadOnly, Range(0f, 1f), FoldoutGroup("Preview")] float _combatRigWeightPreview;
        [ShowInInspector, ReadOnly, Range(0f, 1f), FoldoutGroup("Preview")] float _attackRigWeightPreview;
        
        // Rig Weights
        float _rootRigWeight;
        float _headRigWeight;
        float _bodyRigWeight;
        float _combatRigWeight;
        float _attackRigWeight;

        readonly DialogueAnimationRigging _dialogue = new();
        readonly GlancingAnimationRigging _glancing = new();
        readonly CombatAnimationRigging _combat = new();
        readonly ObservingAnimationRigging _observing = new();
        AnimationRiggingData _inactiveData;

        bool _inBand;
        bool _isArcher;
        INpcInteraction _currentInteraction;

        public bool CanMove { get; private set; }
        
        NpcElement _npcElement;
        NpcElement NPCElement => _npcElement ??= Target.TryGetElement<NpcElement>();

        // === Initialization
        protected override void OnAttach() {
            if (NPCElement == null) {
                Log.Important?.Error($"VCAnimationRiggingHandler attached to a location without an NpcElement! This is not allowed! {(gameObject != null ? gameObject.PathInSceneHierarchy() : "GameObject is null")}", gameObject);
                return;
            }
            NpcCanMoveHandler.AddCanMoveProvider(NPCElement, this);

            _inactiveData = new AnimationRiggingData {
                lookAt = GroundedPosition.HeroPosition,

                headTurnSpeed = defaultHeadRigUpdateSpeed,
                bodyTurnSpeed = defaultBodyRigUpdateSpeed,
                rootTurnSpeed = defaultRootRigUpdateSpeed,
                combatTurnSpeed = defaultCombatRigUpdateSpeed, 
                attackTurnSpeed = defaultCombatRigUpdateSpeed,
            };
            
            Target.GetOrCreateTimeDependent().WithLateUpdate(OnLateUpdate);
            Target.ListenTo(ICullingSystemRegistreeModel.Events.DistanceBandChanged, RefreshDistanceBand, this);
            NPCElement.ListenTo(NpcInteractor.Events.InteractionChanged, OnInteractionChanged, this);
            NPCElement.ListenTo(IAlive.Events.BeforeDeath, _ => Destroy(this), this);
            NPCElement.ListenTo(NpcElement.Events.ItemsAddedToInventory, _ => {
                _isArcher = NPCElement.Inventory.Items.Any(i => i.IsRanged);
            }, this);
            RefreshDistanceBand(Target.GetCurrentBandSafe(0));
            
            _dialogue.Init(this);
            _glancing.Init(this);
            _combat.Init(this);
            _observing.Init(this);
        }

        // === Listener Callbacks
        void RefreshDistanceBand(int band) {
            _inBand = LocationCullingGroup.InAnimationRiggingBand(band);
        }

        void OnInteractionChanged(NpcInteractor.InteractionChangedInfo interactionData) {
            if (interactionData.changeType is NpcInteractor.InteractionChangedInfo.ChangeType.Resume
                or NpcInteractor.InteractionChangedInfo.ChangeType.Start) {
                _currentInteraction = interactionData.interaction;
            } else {
                _currentInteraction = null;
            }
        }

        public void UpdateRigsWeights(Animator animator, AnimancerPlayable playable) {
            RebindAnimatorMarker.Begin();
            animator.Rebind();
            RebindAnimatorMarker.End();
            AnimationRiggingData data = GetAnimationRiggingData();
            UpdateRigsWeights(0, data);
            playable.Evaluate(0);
        }

        // === Updating
        void OnLateUpdate(float deltaTime) {
            if (HasBeenDiscarded || NPCElement == null || NPCElement.HasBeenDiscarded) {
                return;
            }

            _glancing.Update(deltaTime);
            _combat.Update(deltaTime);
            _observing.Update(deltaTime);

            // TODO: add blending
            AnimationRiggingData data = GetAnimationRiggingData();
            
            CanMove = _rootRigWeight < 0.2f && data.rootRigDesiredWeight < 0.1f;
            if (data.lookAt != null) {
                lookAt.position = M.FrameAccurateLerpTo(lookAt.position, data.lookAt.HeadPosition, deltaTime, lookAtTargetSpeed);
            }
            UpdateRigsWeights(deltaTime, data);
        }

        AnimationRiggingData GetAnimationRiggingData() {
            ref readonly AnimationRiggingData data = ref _inactiveData;
            if (_combat.Active) {
                data = ref _combat.Data;
            } else if (_dialogue.Active) {
                data = ref _dialogue.Data;
            } else if (_observing.Active) {
                data = ref _observing.Data;
            } else if (_glancing.Active) {
                data = ref _glancing.Data;
            }
            return data;
        }
        
        void UpdateRigsWeights(float deltaTime, in AnimationRiggingData data) {
            UpdateRigWeight(rootRig, ref _rootRigWeight, data.rootRigDesiredWeight, data.rootTurnSpeed, deltaTime);
            UpdateRigWeight(headRig, ref _headRigWeight, data.headRigDesiredWeight, data.headTurnSpeed, deltaTime);
            UpdateRigWeight(bodyRig, ref _bodyRigWeight, data.bodyRigDesiredWeight, data.bodyTurnSpeed, deltaTime);
            UpdateRigWeight(combatRig, ref _combatRigWeight, data.combatRigDesiredWeight, data.combatTurnSpeed, deltaTime);
            UpdateRigWeight(attackRig, ref _attackRigWeight, data.attackRigDesiredWeight, data.attackTurnSpeed, deltaTime);
            // --- Preview
            UpdatePreview();
        }

        void UpdatePreview() {
            _rootRigWeightPreview = rootRig.weight;
            _headRigWeightPreview = headRig.weight;
            _bodyRigWeightPreview = bodyRig.weight;
            _combatRigWeightPreview = combatRig.weight;
            _attackRigWeightPreview = attackRig.weight;
        }
        
        void UpdateCombatSlider(float desiredValue, float deltaTime) {
            combatIKSlider = M.FrameAccurateLerpTo(combatIKSlider, desiredValue, deltaTime, defaultUpDownTurnSpeed);
        }

        // === Discarding
        protected override void OnDiscard() {
            if (NPCElement is { HasBeenDiscarded: false }) {
                NpcCanMoveHandler.RemoveCanMoveProvider(NPCElement, this);
            }
            Target.GetTimeDependent()?.WithoutLateUpdate(OnLateUpdate);
            
            _npcElement = null;
            _currentInteraction = null;
            
            _dialogue.Dispose();
            _glancing.Dispose();
            _combat.Dispose();
            _observing.Dispose();
            
            Destroy(rootRig);
            Destroy(attackRig);
            Destroy(combatRig);
            Destroy(bodyRig);
            Destroy(headRig);
        }

        // === Helpers

        public void CancelGlancing() => _glancing.Cancel();

        void UpdateRigWeight(Rig rig, ref float currentWeight, float desiredWeight, float updateSpeed, float deltaTime) {
            if (rig != null) {
                currentWeight = Mathf.MoveTowards(currentWeight, desiredWeight, updateSpeed * deltaTime);
                rig.weight = SampleRigWeightOnCurve(currentWeight);
            }
        }
        float SampleRigWeightOnCurve(float passedProxy) {
            return Mathf.Clamp01(rigWeightCurve.Evaluate(passedProxy));
        }
    }
}