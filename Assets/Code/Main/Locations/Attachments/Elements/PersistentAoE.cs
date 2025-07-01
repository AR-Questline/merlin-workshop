using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Executions;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Weapons;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class PersistentAoE : DurationProxy<Location> {
        public override ushort TypeForSerialization => SavedModels.PersistentAoE;

        const float MinDistanceBetweenAoESqr = 0.75f * 0.75f;
        
        static MitigatedExecution MitigatedExecution => World.Services.Get<MitigatedExecution>();
        
        [Saved] protected SphereDamageParameters? _damageParameters;
        [Saved] StatusTemplate _statusTemplate;
        [Saved] SkillVariablesOverride _overrides;
        [Saved] protected float? _tick;
        [Saved] LayerMask _hitMask;
        [Saved] float _buildupStrength;
        [Saved] Dictionary<WeakModelRef<ICharacter>, WeakModelRef<Status>> _characterStatuses = new();
        [Saved] WeakModelRef<ICharacter> _damageDealer;
        [Saved] WeakModelRef<Item> _sourceItem;

        [Saved] PersistentAoEFlags _persistentAoEFlags;
        bool OnlyOnGrounded {
            get => _persistentAoEFlags.HasFlagFast(PersistentAoEFlags.OnlyOnGrounded);
            set {
                if (value) {
                    _persistentAoEFlags |= PersistentAoEFlags.OnlyOnGrounded;
                } else {
                    _persistentAoEFlags &= ~PersistentAoEFlags.OnlyOnGrounded;
                }
            }
        }
        public bool IsRemovingOther {
            get => _persistentAoEFlags.HasFlagFast(PersistentAoEFlags.IsRemovingOther);
            set {
                if (value) {
                    _persistentAoEFlags |= PersistentAoEFlags.IsRemovingOther;
                } else {
                    _persistentAoEFlags &= ~PersistentAoEFlags.IsRemovingOther;
                }
            }
        }
        public bool IsRemovable {
            get => _persistentAoEFlags.HasFlagFast(PersistentAoEFlags.IsRemovable);
            set {
                if (value) {
                    _persistentAoEFlags |= PersistentAoEFlags.IsRemovable;
                } else {
                    _persistentAoEFlags &= ~PersistentAoEFlags.IsRemovable;
                }
            }
        }
        protected bool CanApplyToSelf {
            get => _persistentAoEFlags.HasFlagFast(PersistentAoEFlags.CanApplyToSelf);
            set {
                if (value) {
                    _persistentAoEFlags |= PersistentAoEFlags.CanApplyToSelf;
                } else {
                    _persistentAoEFlags &= ~PersistentAoEFlags.CanApplyToSelf;
                }
            }
        }
        protected bool DiscardParentOnEnd {
            get => _persistentAoEFlags.HasFlagFast(PersistentAoEFlags.DiscardParentOnEnd);
            set {
                if (value) {
                    _persistentAoEFlags |= PersistentAoEFlags.DiscardParentOnEnd;
                } else {
                    _persistentAoEFlags &= ~PersistentAoEFlags.DiscardParentOnEnd;
                }
            }
        }
        protected bool DiscardOnDamageDealerDeath {
            get => _persistentAoEFlags.HasFlagFast(PersistentAoEFlags.DiscardOnDamageDealerDeath);
            set {
                if (value) {
                    _persistentAoEFlags |= PersistentAoEFlags.DiscardOnDamageDealerDeath;
                } else {
                    _persistentAoEFlags &= ~PersistentAoEFlags.DiscardOnDamageDealerDeath;
                }
            }
        }

        IEventListener _damageDealerDeathListener;
        protected float _lastTickTime;
        readonly HashSet<IAlive> _alivesInZone = new();
        readonly List<Action> _damageActions = new();
        public override IModel TimeModel => ParentModel;
        bool AppliesBuildupStatus => _statusTemplate != null && _statusTemplate.IsBuildupAble;
        bool AppliesPersistentStatus => _statusTemplate != null && !_statusTemplate.IsBuildupAble;
        bool IsPositive => _statusTemplate != null && _statusTemplate.IsPositive;
        
        public static PersistentAoE AddPersistentAoE(Location location, float? tick, IDuration duration, StatusTemplate statusTemplate, float buildupStrength,
            SkillVariablesOverride overrides, SphereDamageParameters? sphereDamageParameters, bool onlyOnGrounded, bool isRemovingOther, bool isRemovable, bool canApplyToSelf, bool discardParentOnEnd, bool discardOnDamageDealerDeath) {
            if (location.TryGetElement<PersistentAoE>(out var aoe)) {
                aoe.Discard();
                Log.Minor?.Error($"Adding a new PersistentAoE to a location {location.DebugName} that already has one. Discarding the old one.");
            }
            return location.AddElement(new PersistentAoE(tick, duration, statusTemplate, buildupStrength, overrides, sphereDamageParameters, onlyOnGrounded, isRemovingOther, isRemovable, canApplyToSelf, discardParentOnEnd, discardOnDamageDealerDeath));
        }

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        protected PersistentAoE() { }

        public PersistentAoE(float? tick, IDuration duration, StatusTemplate statusTemplate, float buildupStrength,
            SkillVariablesOverride overrides, SphereDamageParameters? damageParameters, bool onlyOnGrounded, 
            bool isRemovingOther, bool isRemovable, bool canApplyToSelf, bool discardParentOnEnd, bool discardOnDamageDealerDeath) : base(duration) {
            _tick = tick;

            _statusTemplate = statusTemplate;
            _buildupStrength = buildupStrength;
            _overrides = overrides;

            _damageParameters = damageParameters;
            OnlyOnGrounded = onlyOnGrounded;
            
            IsRemovingOther = isRemovingOther;
            IsRemovable = isRemovable;
            CanApplyToSelf = canApplyToSelf;
            DiscardParentOnEnd = discardParentOnEnd;
            DiscardOnDamageDealerDeath = discardOnDamageDealerDeath;
        }
        
        protected override void OnInitialize() {
            if (_tick.HasValue || OnlyOnGrounded) {
                ParentModel.GetOrCreateTimeDependent().WithUpdate(ProcessUpdate);
            }
            
            ParentModel.OnVisualLoaded(OnVisualLoaded);
        }
        
        public void AssignDamageDealer(ICharacter damageDealer) {
            _damageDealer = new WeakModelRef<ICharacter>(damageDealer);
            if (DiscardOnDamageDealerDeath) {
                World.EventSystem.TryDisposeListener(ref _damageDealerDeathListener);
                _damageDealerDeathListener = damageDealer.ListenTo(IAlive.Events.AfterDeath, Discard, this);
            }
        }
        
        [UnityEngine.Scripting.Preserve]
        public void AssignSourceItem(Item sourceItem) {
            _sourceItem = new WeakModelRef<Item>(sourceItem);
        }

        void OnVisualLoaded(Transform t) {
            if (HasBeenDiscarded) {
                return;
            }
            
            if (_damageDealer is { IsSet: true } && DiscardOnDamageDealerDeath) {
                World.EventSystem.TryDisposeListener(ref _damageDealerDeathListener);
                _damageDealerDeathListener = _damageDealer.Get().ListenTo(IAlive.Events.AfterDeath, Discard, this);
            } else if (ParentModel.TryGetElement<ItemBasedLocationMarker>(out var itemBasedLocationMarker)) {
                AssignDamageDealer(itemBasedLocationMarker.Owner);
                _sourceItem = new WeakModelRef<Item>(itemBasedLocationMarker.SourceItem);
            } else if (ParentModel.TryGetElement(out NpcElement npc)) {
                DiscardOnDamageDealerDeath = true;
                AssignDamageDealer(npc);
            }
            
            VerifyPosition();
        }

        void VerifyPosition() {
            if (HasBeenDiscarded) {
                return;
            }
            if (!IsRemovingOther) {
                return;
            }
            
            foreach (var other in World.All<PersistentAoE>().ToArraySlow()) {
                if (other == this) {
                    continue;
                }

                if ((other.ParentModel.Coords - ParentModel.Coords).sqrMagnitude <= MinDistanceBetweenAoESqr) {
                    other.NewPersistentAoEInRange();
                }
            }
        }

        protected virtual void ProcessUpdate(float deltaTime) {
            ApplyRemoveStatus();

            if (!_tick.HasValue) {
                return;
            }
            
            _lastTickTime -= deltaTime;
            if (_lastTickTime <= 0) {
                ApplyOverTimeEffects();
            }
        }

        protected void ApplyRemoveStatus() {
            if (OnlyOnGrounded && AppliesPersistentStatus) {
                foreach (var alive in _alivesInZone) {
                    if (alive is not ICharacter character) {
                        continue;
                    }
                    bool hasKey = _characterStatuses.ContainsKey(new WeakModelRef<ICharacter>(character));
                    if (character.Grounded && !hasKey) {
                        ApplyPersistentStatus(character);
                    } else if (!character.Grounded && hasKey) {
                        RemovePersistentStatus(character);
                    }
                }
            }
        }

        protected void ApplyOverTimeEffects() {
            _alivesInZone.RemoveWhere(static a => a.HasBeenDiscarded || a.IsDying);
            
            if (_damageParameters is { rawDamageData: not null } damageParameters) {
                DealDamage(damageParameters);
            }
            
            if (AppliesBuildupStatus) {
                foreach (var alive in _alivesInZone) {
                    ApplyBuildupStatus(alive);
                }
            }
            
            _lastTickTime = _tick!.Value;
        }

        void DealDamage(SphereDamageParameters sphereDamageParameters) {
            _damageActions.Clear();
            foreach (var aliveInZone in _alivesInZone) {
                if (OnlyOnGrounded && !aliveInZone.Grounded) {
                    continue;
                }
                
                Action damageAction = DealDamageAction(aliveInZone, sphereDamageParameters);
                if (damageAction != null) {
                    _damageActions.Add(damageAction);
                }
            }
            float tick = _tick ?? 0.1f;
            MitigatedExecution.RegisterOverTime(_damageActions, tick, this, MitigatedExecution.Cost.Heavy, MitigatedExecution.Priority.High, 0.1f);
        }

        Action DealDamageAction(IAlive receiver, SphereDamageParameters input) {
            if (receiver != null && receiver.TryGetElement(out HealthElement healthElement)) {
                DamageParameters parameters = input.baseDamageParameters;
                parameters.IsPrimary = false;
                parameters.IsDamageOverTime = _tick is < 1f;
                parameters.Position = receiver.Coords;
                Vector3 horizontalDirection = receiver.Coords.ToHorizontal3() - ParentModel.Coords.ToHorizontal3();
                parameters.Direction = horizontalDirection + Vector3.up;
                parameters.ForceDirection = parameters.Direction;
                float damageAmount = input.rawDamageData.CalculatedValue;
                Damage damage = new(parameters, _damageDealer.Get(), receiver, new RawDamageData(damageAmount));
                if (_sourceItem.TryGet(out var item)) {
                    damage.WithItem(item);
                }
                return () => healthElement.TakeDamage(damage);
            }
            return null;
        }

        void ApplyBuildupStatus(IAlive receiver) {
            if (OnlyOnGrounded && !receiver.Grounded) {
                return;
            }
            
            StatusSourceInfo statusSourceInfo = StatusSourceInfo.FromStatus(_statusTemplate);
            if (receiver is ICharacter character) {
                VGUtils.ApplyStatus(character.Statuses, _statusTemplate, statusSourceInfo, _buildupStrength, null, _overrides);
                
                if (_damageDealer.TryGet(out ICharacter damageDealer) && !IsPositive && character is NpcElement npcElement) {
                    npcElement.NpcAI.ReceiveHostileAction(damageDealer, null, DamageType.Trap);
                }
            }
        }

        void ApplyPersistentStatus(IAlive alive) {
            if (alive is ICharacter character) {
                ApplyPersistentStatus(character);
            }
        }
        
        void ApplyPersistentStatus(ICharacter character) {
            StatusSourceInfo statusSourceInfo = StatusSourceInfo.FromStatus(_statusTemplate);
            if (_damageDealer.TryGet(out var applier)) {
                statusSourceInfo = statusSourceInfo.WithCharacter(applier);
            }
            if (_characterStatuses.Keys.All(c => c.Get() != character)) {
                var result = VGUtils.ApplyStatus(character.Statuses, _statusTemplate, statusSourceInfo, _buildupStrength, new UntilDiscarded(), _overrides);
                _characterStatuses[new WeakModelRef<ICharacter>(character)] = result.newStatus;
                
                if (_damageDealer.TryGet(out ICharacter damageDealer) && !IsPositive && character is NpcElement npcElement) {
                    npcElement.NpcAI.ReceiveHostileAction(damageDealer, null, DamageType.Trap);
                }
            }
        }

        void RemovePersistentStatus(IAlive alive) {
            if (alive is ICharacter character) {
                RemovePersistentStatus(character);
            }
        }
        
        void RemovePersistentStatus(ICharacter character) {
            var key = new WeakModelRef<ICharacter>(character);
            if (_characterStatuses.TryGetValue(key, out var statusRef) && statusRef.TryGet(out Status status)) {
                character.Statuses.RemoveStatus(status);
                _characterStatuses.Remove(key);
            }
        }

        public void AliveEnteredZone(IAlive alive) {
            if (!CanApplyToSelf && alive == _damageDealer.Get()) {
                return;
            }
            
            if (!_alivesInZone.Add(alive)) {
                return;
            }
            
            if (AppliesPersistentStatus && (!OnlyOnGrounded || alive.Grounded)) {
                ApplyPersistentStatus(alive);
            }
            
            ParentModel.TriggerVisualScriptingEvent("AliveEnteredZone", alive);
        }

        public void AliveExitedZone(IAlive alive) {
            _alivesInZone.Remove(alive);

            if (AppliesPersistentStatus) {
                RemovePersistentStatus(alive);
            }
            
            ParentModel.TriggerVisualScriptingEvent("AliveExitedZone", alive);
        }

        public void NewPersistentAoEInRange() {
            if (IsRemovable) {
                End();
            }
        }
        
        protected override void OnDurationElapsed() {
            ParentModel.GetTimeDependent()?.WithoutUpdate(ProcessUpdate);

            End();
        }

        protected virtual void End() {
            if (ParentModel.HasBeenDiscarded) {
                return;
            }
            
            if (DiscardParentOnEnd) {
                ParentModel.Discard();
            } else {
                Discard();
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            ParentModel.GetTimeDependent()?.WithoutUpdate(ProcessUpdate);
            
            if (fromDomainDrop) {
                return;
            }
            
            foreach ((WeakModelRef<ICharacter> characterRef, WeakModelRef<Status> statusRef) in _characterStatuses) {
                if (statusRef.TryGet(out Status status) 
                    && characterRef.TryGet(out ICharacter character) 
                    && !character.HasBeenDiscarded) {
                    character.Statuses.RemoveStatus(status);
                }
            }
            _characterStatuses.Clear();
        }

        [Flags]
        public enum PersistentAoEFlags : byte {
            OnlyOnGrounded = 1 << 0,
            IsRemovingOther = 1 << 1,
            IsRemovable = 1 << 2,
            CanApplyToSelf = 1 << 3,
            DiscardParentOnEnd = 1 << 4,
            DiscardOnDamageDealerDeath = 1 << 5
        }
    }
}