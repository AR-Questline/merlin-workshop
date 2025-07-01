using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Awaken.TG.Assets;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.AI.States;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.StateMachines;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Main.AI.Combat.Behaviours.MagicBehaviours {
    [Serializable]
    public partial class ResurrectAllyBehaviour : SpellCastingBehaviourBase {
        [BoxGroup(BaseCastingGroup), SerializeField] bool connectFireballInHandToTarget;
        [BoxGroup(SpellEffectVisualsGroup), SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.Weapons)]
        ShareableARAssetReference connectVfxToTarget;
        [BoxGroup(SpellEffectVisualsGroup), SerializeField, HideIf(nameof(connectingUseAdditionalHand))] CastingHand connectingCastingHand = CastingHand.OffHand;
        [BoxGroup(SpellEffectVisualsGroup), SerializeField] bool connectingUseAdditionalHand;
        [BoxGroup(SpellEffectVisualsGroup), SerializeField, ShowIf(nameof(connectingUseAdditionalHand))] AdditionalHand connectingAdditionalHand;
        [BoxGroup(SpellEffectVisualsGroup), SerializeField] Vector3 connectingCastingPointOffset = Vector3.zero;
        [BoxGroup(SpellEffectGroup), SerializeField] int maxAlliesToResurrectAtOnce = 1;
        [BoxGroup(SpellEffectGroup), SerializeField] bool requireNpcAbstracts;
        [BoxGroup(SpellEffectGroup), SerializeField, ShowIf(nameof(requireNpcAbstracts)), TemplateType(typeof(Template))]
        TemplateReference[] requireAnyAbstractType = Array.Empty<TemplateReference>();
        [BoxGroup(SpellEffectVisualsGroup), SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.Weapons)]
        ShareableARAssetReference castVfx;
        [BoxGroup(SpellEffectVisualsGroup), SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.Weapons)]
        ShareableARAssetReference beingDeadNpcVfx;
        
        CombatLeaveBlockerUntilAllResurrected _combatLeaveBlockerUntilAllResurrected;
        List<WaitingToBeResurrectedData> _deadAllyData;
        List<WaitingToBeResurrectedData> _beingResurrected;
        IEventListener _beforeTakenFinalDamageListener;
        bool _active;

        protected override void OnInitialize() {
            base.OnInitialize();
            _deadAllyData = new();
            _beingResurrected = new();
            Npc.ListenTo(NpcAI.Events.NpcStateChanged, OnStateChanged, this);
        }

        protected override bool StartBehaviour() {
            _beingResurrected.Clear();
            FindTargets();
            return base.StartBehaviour();
        }
        
        protected override MovementState GetDesiredMovementState() {
            return rotateToTarget ? new NoMoveAndRotateTowardsCustomTarget(_beingResurrected.Count > 0 ? _beingResurrected[0].element.ParentModel : null) : new NoMove();
        }

        public override void Update(float deltaTime) {
            base.Update(deltaTime);
            UpdateVfxConnectPositions();
        }
        
        public override void StopBehaviour() {
            base.StopBehaviour();
            ResetCurrentTargets();
        }

        public override void BehaviourInterrupted() {
            base.BehaviourInterrupted();
            ResetCurrentTargets();
        }

        public override bool UseConditionsEnsured() {
            return base.UseConditionsEnsured() && TargetExists();

            bool TargetExists() {
                foreach (var data in _deadAllyData) {
                    if (data.element is { IsBeingResurrected: false }) {
                        return true;
                    }
                }
                return false;
            }
        }

        protected override async UniTask SpawnFireBallInHand() {
            await base.SpawnFireBallInHand();
            if (HasBeenDiscarded) {
                return;
            }
            if (!connectFireballInHandToTarget || _beingResurrected.Count <= 0 || _fireBallInstance.Instance == null || !_fireBallInstance.Instance.TryGetComponent(out VisualEffect vfx)) {
                return;
            }
            vfx.SetVector3("TargetPosition", GetConnectVfxHand().position);
        }

        protected override UniTask CastSpell(bool returnFireballInHandAfterSpawned = true) {
            foreach (var beingResurrected in _beingResurrected) {
                beingResurrected.element.Resurrect();
                beingResurrected.BeforeRemove();
            }
            _beingResurrected.Clear();

            if (castVfx is { IsSet: true }) {
                PrefabPool.InstantiateAndReturn(castVfx, GetSpellPosition(), Quaternion.identity).Forget();
            }

            if (returnFireballInHandAfterSpawned) {
                ReturnInstantiatedPrefabs();
            }
            
            if (_deadAllyData.Count == 0) {
                _combatLeaveBlockerUntilAllResurrected?.Discard();
                _combatLeaveBlockerUntilAllResurrected = null;
            }

            return UniTask.CompletedTask;
        }
        
        // Allies Targeting
        void FindTargets() {
            var validTargets = _deadAllyData.Where(e => e.element is { IsBeingResurrected: false }).ToArray();

            if (validTargets.Length == 0) {
                StopBehaviour();
                return;
            }
            
            if (validTargets.Length <= maxAlliesToResurrectAtOnce) {
                _beingResurrected.AddRange(validTargets);
            } else {
                _beingResurrected.AddRange(validTargets.OrderBy(x => RandomUtil.UniformFloat(0, 1)).Take(maxAlliesToResurrectAtOnce));
            }
            
            foreach (var deadAlly in _beingResurrected) {
                deadAlly.element.ResurrectionStarted();
                UpdateResurrectConnectVfxState(deadAlly, true);
            }
        }

        void ResetCurrentTargets() {
            foreach (var beingResurrected in _beingResurrected) {
                beingResurrected.element.StopResurrecting();
                UpdateResurrectConnectVfxState(beingResurrected, false);
            }
            _beingResurrected.Clear();
        }
        
        // Passive Behaviour

        void OnStateChanged(Change<IState> change) {
            switch (change) {
                //If AI/Idle is disabled it will stop interactions
                case (StateAIWorking, _) or (StateCombat, not null):
                    OnBehaviourDisabled();
                    break;
                case (_, StateCombat):
                    OnCombatEntered();
                    break;
            }
        }
        
        void OnBehaviourDisabled() {
            if (!_active) {
                return;
            }
            _active = false;
            bool isStillAliveAndActive = ParentModel is { HasBeenDiscarded: false} && Npc is { HasBeenDiscarded: false, IsAlive: true, IsDying: false, IsUnconscious: false } && !Npc.HasElement<NpcDisappearedElement>();
            if (isStillAliveAndActive) {
                OnBehaviourDisabledInternal();
            } else {
                OnBeingKilled();
            }
            World.EventSystem.TryDisposeListener(ref _beforeTakenFinalDamageListener);
            if (_combatLeaveBlockerUntilAllResurrected != null) {
                _combatLeaveBlockerUntilAllResurrected.Discard();
                _combatLeaveBlockerUntilAllResurrected = null;
            }
        }
        
        void OnBehaviourDisabledInternal() {
            if (_deadAllyData.Count > 0) {
                foreach (var deadAlly in _deadAllyData) {
                    World.EventSystem.RemoveListener(deadAlly.listener);
                    deadAlly.element.Resurrect();
                    deadAlly.BeforeRemove();
                }
                _deadAllyData.Clear();
            }
        }
        
        void OnBeingKilled() {
            if (_deadAllyData.Count > 0) {
                foreach (var deadAlly in _deadAllyData) {
                    World.EventSystem.RemoveListener(deadAlly.listener);
                    deadAlly.element.RemoveResurrector(Npc);
                    deadAlly.BeforeRemove();
                }
                _deadAllyData.Clear();
            }
        }

        void OnCombatEntered() {
            if (_active) {
                return;
            }
            _active = true;
            _beforeTakenFinalDamageListener ??= World.EventSystem.ListenTo(EventSelector.AnySource, HealthElement.Events.BeforeTakenFinalDamage, this, OnBeforeAnyAllyTookFinalDamage);
        }

        void OnBeforeAnyAllyTookFinalDamage(HookResult<HealthElement, Damage> hook) {
            float hpAfterDamage = hook.Model.Health.ModifiedValue - hook.Value.Amount;
            if (hpAfterDamage > 0f) {
                return;
            }
            if (hook.Model.ParentModel is not NpcElement npc || npc == Npc) {
                return;
            }
            if (!IsValidFriendlyNpc(npc)) {
                return;
            }

            if (_deadAllyData.Count == 0) {
                _combatLeaveBlockerUntilAllResurrected ??= Npc.AddElement<CombatLeaveBlockerUntilAllResurrected>();
            }

            if (npc.TryGetElement(out WaitingToBeResurrectedElement waitingToBeResurrectedElement)) {
                // Handling 2 or more resurrecting NPCs
                waitingToBeResurrectedElement.AddResurrector(Npc);
            } else {
                waitingToBeResurrectedElement = new WaitingToBeResurrectedElement(hook.Value.DamageDealer, Npc, beingDeadNpcVfx);
                npc.AddElement(waitingToBeResurrectedElement);
            }

            if (_deadAllyData.All(d => d.element != waitingToBeResurrectedElement)) {
                var listener = waitingToBeResurrectedElement.ListenTo(Model.Events.BeforeDiscarded, OnElementDiscard, this);
                var data = new WaitingToBeResurrectedData(waitingToBeResurrectedElement, listener);
                _deadAllyData.Add(data);
                if (connectVfxToTarget is { IsSet: true }) {
                    AddVfxToTarget(data).Forget();
                }
            }
            
            hook.Prevent();
        }

        bool IsValidFriendlyNpc(NpcElement npc) {
            if (!Npc.IsFriendlyTo(npc)) {
                return false;
            }
            if (requireNpcAbstracts) {
                bool any = requireAnyAbstractType.Any(a => npc.Template.InheritsFrom(a.Get<Template>()));
                if (!any) {
                    return false;
                }
            }
            return true;
        }
        
        // === VFX

        async UniTaskVoid AddVfxToTarget(WaitingToBeResurrectedData data) {
            Transform hand = GetConnectVfxHand();
            data.cancellationToken = new CancellationTokenSource();
            data.vfxInstance = await PrefabPool.Instantiate(connectVfxToTarget, connectingCastingPointOffset, Quaternion.identity, hand, cancellationToken: data.cancellationToken.Token);
            if (data.vfxInstance.Instance == null || !data.vfxInstance.Instance.TryGetComponent(out VisualEffect vfx)) {
                return;
            }
            vfx.SetVector3("TargetPosition", data.element.ParentModel.Hips.position);
            UpdateResurrectConnectVfxState(data, false);
        }
        
        Transform GetConnectVfxHand() {
            if (_deadAllyData[0]?.vfxInstance?.Instance?.transform is { } transform) {
                return transform.parent;
            } else if (connectingUseAdditionalHand) {
                var hand = ParentModel.GetAdditionalHand(connectingAdditionalHand);
                if (hand != null) {
                    return hand;
                }
                Log.Minor?.Error($"{Npc.Name} has no additional hand at ID {connectingAdditionalHand}");
                return Npc.MainHand;
            } else {
                return connectingCastingHand == CastingHand.MainHand ? Npc.MainHand : Npc.OffHand;
            }
        }

        void UpdateResurrectConnectVfxState(WaitingToBeResurrectedData data, bool beingResurrected) {
            data.vfx?.SetBool("EffectTriggered", beingResurrected);
        }

        void UpdateVfxConnectPositions() {
            if (connectFireballInHandToTarget && _beingResurrected.Count > 0) {
                if (connectVfxToTarget is { IsSet: true } && _deadAllyData.Count > 0) {
                    _fireBallInstance?.Instance?.GetComponent<VisualEffect>()?.SetVector3("TargetPosition", GetConnectVfxHand().position);
                } else if (_beingResurrected.Count > 0) {
                    _fireBallInstance?.Instance?.GetComponent<VisualEffect>()?.SetVector3("TargetPosition", _beingResurrected[0].element.ParentModel.Hips.position);
                }
            }
            foreach (var deadAlly in _deadAllyData) {
                deadAlly.vfx?.SetVector3("TargetPosition", deadAlly.element.ParentModel.Hips.position);
            }
        }

        // === Discarding

        void OnElementDiscard(Model model) {
            var data = _deadAllyData.First(d => d.element == (WaitingToBeResurrectedElement)model);
            data.BeforeRemove();
            _deadAllyData.Remove(data);
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            if (!fromDomainDrop) {
                OnBehaviourDisabled();
            }
            base.OnDiscard(fromDomainDrop);
        }
    }

    public partial class CombatLeaveBlockerUntilAllResurrected : Element<NpcElement>, INpcCombatLeaveBlocker {
        public override ushort TypeForSerialization => SavedModels.CombatLeaveBlockerUntilAllResurrected;

        public bool BlocksCombatExit => true;
    }

    internal class WaitingToBeResurrectedData {
        public WaitingToBeResurrectedElement element;
        public IEventListener listener;
        public IPooledInstance vfxInstance;
        public VisualEffect vfx;
        public CancellationTokenSource cancellationToken;
        
        public WaitingToBeResurrectedData(WaitingToBeResurrectedElement element, IEventListener listener) {
            this.element = element;
            this.listener = listener;
        }

        public void BeforeRemove() {
            cancellationToken?.Cancel();
            cancellationToken = null;
            vfxInstance?.Return();
            vfxInstance = null;
        }
    }
}