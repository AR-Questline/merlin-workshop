using System.Linq;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.AI;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.SummonsAndAllies;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments.Audio;
using Awaken.TG.Main.Heroes.Items.Weapons;
using Awaken.TG.Main.Heroes.MovementSystems;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.Main.Utility.Semaphores;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Main.Utility.VFX;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using FMODUnity;
using Unity.VisualScripting;
using UnityEngine;
using QuickSelect = Awaken.TG.Utility.Maths.QuickSelect;

namespace Awaken.TG.Main.Character {
    public partial class HealthElement : Element<IAlive> {
        public sealed override bool IsNotSaved => true;

        const int MaxHitboxCollisionCount = 10;
        const float DoTHitAudioCooldown = 2f;

        // === Fields & Properties
        public LimitedStat Health => ParentModel.Health;
        public Stat MaxHealth => ParentModel.MaxHealth;
        public bool IsDead { get; private set; }
        Vector3 Position => ParentModel.Coords;
        
        bool _initialized;
        HitboxWrapper _hitboxes;
        RaycastHit[] _raycastHits;
        RaycastHitComparer _raycastHitComparer;
        IEventMachine[] _eventMachines;
        GameObject[] _machineOwners;
        FragileSemaphore _dotAudioSemaphore;

        // === Events
        public new static class Events {
            public static readonly Event<HealthElement, DamageOutcome> OnDamageTaken = new(nameof(OnDamageTaken));
            public static readonly Event<ICharacter, DamageOutcome> OnDamageDealt = new(nameof(OnDamageDealt));
            public static readonly Event<ICharacter, DamageOutcome> OnSneakDamageDealt = new(nameof(OnSneakDamageDealt));
            public static readonly Event<ICharacter, DamageOutcome> OnKill = new(nameof(OnKill));
            public static readonly Event<ICharacter, DamageOutcome> OnHeroSummonKill = new(nameof(OnHeroSummonKill));

            public static readonly Event<ICharacter, Damage> BeforeDamageMultiplied = new(nameof(BeforeDamageMultiplied));
            public static readonly Event<ICharacter, Damage> BeforeDamageTakenMultiplied = new(nameof(BeforeDamageTakenMultiplied));
            public static readonly Event<ICharacter, ModifiedDamageInfo> OnDamageMultiplied = new(nameof(OnDamageMultiplied));
            public static readonly Event<ICharacter, ModifiedDamageInfo> OnDamageTakenMultiplied = new(nameof(OnDamageTakenMultiplied));
            public static readonly Event<ICharacter, Damage> OnMyDamageBlocked = new(nameof(OnMyDamageBlocked));
            public static readonly Event<HealthElement, Damage> OnDamageParried = new(nameof(OnDamageParried));
            public static readonly Event<HealthElement, Damage> OnDamageBlocked = new(nameof(OnDamageBlocked));
            public static readonly Event<HealthElement, Damage> BeforeDamageParried = new(nameof(BeforeDamageParried));
            public static readonly Event<HealthElement, Damage> BeforeDamageBlocked = new(nameof(BeforeDamageBlocked));
            public static readonly Event<HealthElement, Damage> BeforeDamageTaken = new(nameof(BeforeDamageTaken));
            public static readonly Event<ICharacter, Damage> BeforeDamageDealt = new(nameof(BeforeDamageDealt));
            public static readonly Event<ICharacter, DamageModifiersInfo> OnModifiedDamageDealt = new(nameof(OnModifiedDamageDealt));
            public static readonly Event<ICharacter, DamageModifiersInfo> OnWeakspotHit = new(nameof(OnWeakspotHit));

            public static readonly HookableEvent<ICharacter, Damage> DealingDamage = new(nameof(DealingDamage));
            public static readonly HookableEvent<HealthElement, Damage> TakingDamage = new(nameof(TakingDamage));
            public static readonly HookableEvent<HealthElement, Damage> KillPreventionBeforeTakenFinalDamage = new(nameof(KillPreventionBeforeTakenFinalDamage));
            public static readonly HookableEvent<HealthElement, Damage> BeforeTakenFinalDamage = new(nameof(BeforeTakenFinalDamage));
            
            public static readonly Event<HealthElement, Damage> DamagePreventedByHitbox = new(nameof(DamagePreventedByHitbox));
            public static readonly Event<HealthElement, Damage> DamagePreventedByHook = new(nameof(DamagePreventedByHook));
        }

        // === Initialization
        protected override void OnInitialize() {
            if (ParentModel is ILocationElement locationElement) {
                locationElement.ParentModel.OnVisualLoaded(Init);
            } else {
                Init(ParentModel.ParentTransform);
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (!fromDomainDrop && ParentModel is {HasBeenDiscarded: false}) {
                ParentModel.GetTimeDependent()?.WithoutUpdate(SemaphorUpdate);
            }
        }

        void Init(Transform parentTransform) {
            _eventMachines = parentTransform.GetComponentsInChildren<IEventMachine>(true);
            var graphPointers = _eventMachines.Select(m => m.GetReference()).WhereNotNull();
            _machineOwners = graphPointers.Select(p => p.gameObject).Distinct().ToArray();

            _hitboxes.EnsureInitialized();
            if (parentTransform.TryGetComponentInChildren(out AliveComponent aliveComponent)) {
                _hitboxes.SetDefaultHitboxes(aliveComponent.hitboxes);
            }

            _raycastHits = new RaycastHit[MaxHitboxCollisionCount];
            _raycastHitComparer = new RaycastHitComparer();

            _initialized = true;
        }

        void SemaphorUpdate(float deltaTime) {
            _dotAudioSemaphore.Update();
        }

        public void AddHitbox(Collider collider, in HitboxData data) {
            _hitboxes.EnsureInitialized();
            _hitboxes.AddHitbox(collider, data);
        }

        public void RemoveHitbox(Collider collider) {
            _hitboxes.RemoveHitbox(collider);
        }
        
        public void Revive() {
            Health.SetToFull();
            IsDead = false;
        }
        public void TakeDamage(Damage damage) {
            if (!_initialized || IsDead || HasBeenDiscarded) {
                return;
            }

            TakeDamageInternal(damage);
        }

        protected virtual void TakeDamageInternal(Damage damage) {
            damage = damage.WithPosition(Position).WithDirection(Vector3.zero);
            OnDamage(damage);
        }
        
        void OnDamage(Damage damage) {
            damage.ApplyBeforeDamageMultipliedModifiers();
            Vector3 position = damage.Position ?? Position;
            Vector3 direction;
            if (damage.Direction.HasValue && damage.Direction.Value != Vector3.zero) {
                direction = damage.Direction.Value;
            } else {
                direction = ParentModel.ParentTransform.forward;
            }
            
            DamageMultiplierResult result = AddDamageMultiplier(position, direction, damage.Radius, damage.HitCollider, damage.Item);
            damage.HitCollider = result.colliderHit;
            damage.WeakSpotMultiplier = result.weakSpotMultiplier;
            damage.WeakSpotHit = damage.WeakSpotMultiplier > 1f;

            // Run hooks
            if (damage.DamageDealer != null) {
                var dealingHookResult = Events.DealingDamage.RunHooks(damage.DamageDealer, damage);
                if (dealingHookResult.Prevented) {
                    this.Trigger(Events.DamagePreventedByHook, damage);
                    return;
                }
            }

            var hookResult = Events.TakingDamage.RunHooks(this, damage);
            if (hookResult.Prevented) {
                this.Trigger(Events.DamagePreventedByHook, damage);
                return;
            }

            // --- Hitbox
            ref readonly HitboxData hitbox = ref _hitboxes.GetHitbox(damage.HitCollider, out var hitboxExists);

            if (hitboxExists && hitbox.CanPreventDamage(damage.Item)) {
                this.Trigger(Events.DamagePreventedByHitbox, damage);
                return;
            }
            
            // --- Armor and Damage Type
            float armorAndDamageTypeMultiplier = damage.DamageTypeData.CalculateMultiplier(ref damage, ParentModel);
            damage.RawData.MultiplyMultModifier(armorAndDamageTypeMultiplier);

            if (damage.Type is DamageType.Trap) {
                damage.RawData.MultiplyMultModifier(ParentModel.AliveStats.TrapDamageMultiplier.ModifiedValue);
            }

            DamageModifiersInfo dmgModifiersInfo = ApplyDamageModifiers(damage, out float dmgModifier);
            damage.RawData.MultiplyMultModifier(dmgModifier);
            damage.ApplyOnDamageMultipliedModifiers(dmgModifiersInfo);
            
            // Apply Incoming damage stat modifier
            ICharacter character = ParentModel as ICharacter;
            if (character != null) {
                var multiplier = character.CharacterStats.IncomingDamage;
                if (multiplier <= 0) return;
                damage.RawData.MultiplyMultModifier(multiplier);
            }
            
            // Before final hook check all kill preventions with correct (not random) priority.
            hookResult = Events.KillPreventionBeforeTakenFinalDamage.RunHooks(this, damage);
            if (hookResult.Prevented) {
                this.Trigger(Events.DamagePreventedByHook, damage);
                return;
            }
            
            // Final hook with calculated damage amount that still can prevent taking damage
            hookResult = Events.BeforeTakenFinalDamage.RunHooks(this, damage);
            if (hookResult.Prevented) {
                this.Trigger(Events.DamagePreventedByHook, damage);
                return;
            }

            if (dmgModifiersInfo.AnyCritical && damage.DamageDealer != null) {
                damage.DamageDealer.Trigger(Events.OnModifiedDamageDealt, dmgModifiersInfo);
            }

            if (dmgModifiersInfo.IsWeakSpot && damage.DamageDealer != null) {
                damage.DamageDealer.Trigger(Events.OnWeakspotHit, dmgModifiersInfo);
            }

            // Consume some damage if we have mana shield
            HandleManaShield(damage, out var damageToMana);

            // Send before events
            BeforeHealthDecreaseEvents(damage);
            if (HasBeenDiscarded) return;
            
            // Decrement the stamina by taken stamina damage.
            if (damage.StaminaDamageAmount > 0 && character != null) {
                ContractContext context = new(damage.DamageDealer, ParentModel, ChangeReason.CombatDamage);
                character.CharacterStats.Stamina.DecreaseBy(damage.StaminaDamageAmount, context);
            }

            // Decrement the health by taken damage.
            damage.RawData.FinalCalculation();
            damage.DamageTypeData.FinalizeDamage(damage.Amount);
            float healthBeforeDecrease = Health.ModifiedValue;
            Health.DecreaseBy(damage.Amount);

            DamageOutcome outcome = new(damage, position, dmgModifiersInfo, healthBeforeDecrease - Health.ModifiedValue);
            
            bool isKilled = Health.ModifiedValue <= 0 && !IsDead;

            // Send after events
            AfterHealthDecreaseEvents(outcome);
            if (HasBeenDiscarded) return;

            _machineOwners.ForEach(m => {
                CustomEvent.Trigger(m, "GetHit", position, direction, damage.DamageDealer?.ParentTransform);
                if (damage.Amount <= 0) {
                    CustomEvent.Trigger(m, "DamageBlocked", damage.RawData.UncalculatedValue);
                }
            });
            bool triggerDeathEvents = false;
            if (isKilled) {
                OnKillingDamage(damage);
                triggerDeathEvents = true;
            } else {
                if (!_dotAudioSemaphore.IsValid) {
                    _dotAudioSemaphore = new FragileSemaphore(true, null, DoTHitAudioCooldown, true);
                    ParentModel.GetOrCreateTimeDependent().WithUpdate(SemaphorUpdate);
                }
                HurtAudio(damage, ref _dotAudioSemaphore);
            }
            
            // --- Retaliation
            if (damage.Parameters.IsPrimary && damage.DamageDealer is {IsAlive: true, IsDying: false} && ParentModel is ICharacter icharacter && damage.DamageDealer != icharacter) {
                HandleManaShieldRetaliation(icharacter, damage.DamageDealer, damageToMana);
                HandleMeleeRetaliation(icharacter, damage.DamageDealer, damage.Item, damage.Amount / armorAndDamageTypeMultiplier);
            }

            // --- HitSurface
            SurfaceType hitSurface = GetHitSurface(damage, hitbox, hitboxExists);
            // --- VFX
            NpcElement npc = ParentModel as NpcElement;
            if (!damage.IsDamageOverTime && !damage.IsBlocked) {
                SpawnOnHitEffect(position, direction, damage.HitCollider);
            }

            if (damage.Item != null && !damage.IsDamageOverTime) {
                VFXManager.SpawnCombatVFX(damage.DamageSurfaceType, hitSurface, position, direction, ParentModel, damage.HitCollider);
                // --- If this is critical spawn additional critical VFX.
                if (!damage.IsBlocked && npc != null) {
                    bool useCriticalVfx = (dmgModifiersInfo.IsCritical || dmgModifiersInfo.IsWeakSpot) && npc.CriticalVFX.IsSet;
                    if (useCriticalVfx) {
                        VFXManager.SpawnCombatVFX(npc.CriticalVFX, position, direction, damage.HitCollider).Forget();
                    } else if (dmgModifiersInfo.IsBackStab && npc.BackStabVFX.IsSet) {
                        VFXManager.SpawnCombatVFX(npc.BackStabVFX, position, direction, damage.HitCollider).Forget();
                    } 
                }
            }

            if (IsDead && npc != null && npc.DeathVFX.IsSet && npc.ShouldSpawnDeathVFX && !damage.IsDamageOverTime) {
                VFXManager.SpawnCombatVFX(npc.DeathVFX, position, direction, damage.HitCollider).Forget();
            }
            // --- Hit Audio
            if (hitSurface != null) {
                HitAudio(damage, hitSurface, dmgModifiersInfo, IsDead);
            } else {
                Log.Important?.Error($"Missing HitSurface for {LogUtils.GetDebugName(damage.DamageDealer)} attacking {LogUtils.GetDebugName(damage.Target)} with item {LogUtils.GetDebugName(damage.Item)}");
            }
            // --- Vibration
            bool isHero = ParentModel is Hero;
            if (damage.IsPrimary && isHero) {
                RewiredHelper.VibrateLowFreq(VibrationStrength.Medium, VibrationDuration.Medium);
            }

            if (triggerDeathEvents) {
                OnDeathEvents(outcome);
                return;
            }

            if (npc?.SalsaEmitter != null) {
                npc.SalsaEmitter.TriggerEmotion(SalsaEmotion.Hurt);
            }

            bool knockDown = damage.Parameters.KnockdownType is KnockdownType.OnDamageTaken or KnockdownType.Always;
            
            if (knockDown && npc) {
                npc.CharacterStats.Stamina.SetTo(0, false,
                    new ContractContext(damage.DamageDealer, ParentModel, ChangeReason.CombatDamage));
            } else if (isHero) {
                bool heroMountedKnockdown = Hero.Current.MovementSystem is MountedMovement &&
                                            !damage.IsDamageOverTime && damage.IsPrimary;
                if (knockDown || heroMountedKnockdown) {
                    HeroKnockdown.EnterKnockdown(damage.DamageDealer, damage.ForceDirection ?? Vector3.zero,
                        damage.Parameters.KnockdownStrength);
                }
            }
        }

        public void Kill(ICharacter killer = null, bool allowPrevention = false) {
            if (IsDead) {
                return;
            }

            Damage damage = new(DamageParameters.Default, killer, ParentModel, new RawDamageData(Health.ModifiedValue));
            if (allowPrevention) {
                if (KillPreventionDispatcher.HasActivePrevention(ParentModel)) {
                    var hookResult = Events.KillPreventionBeforeTakenFinalDamage.RunHooks(this, damage);
                    if (hookResult.Prevented) {
                        this.Trigger(Events.DamagePreventedByHook, damage);
                        return;
                    }
                }
            }
            
            Health.SetTo(0f);
            
            OnKillingDamage(damage);

            if (ParentModel is NpcElement npc && npc.DeathVFX.IsSet && npc.ShouldSpawnDeathVFX) {
                Vector3 position = damage.Position ?? Position;
                Vector3 direction;
                if (damage.Direction.HasValue && damage.Direction.Value != Vector3.zero) {
                    direction = damage.Direction.Value;
                } else {
                    direction = Vector3.forward;
                }
                VFXManager.SpawnCombatVFX(npc.DeathVFX, position, direction, damage.HitCollider).Forget();
            }
            
            DamageOutcome outcome = new(damage, ParentModel.Coords, new DamageModifiersInfo(), Health.ModifiedValue);
            OnDeathEvents(outcome);
        }

        public void KillFromFinisher(DamageOutcome outcome) {
            OnKillingDamage(outcome.Damage, true);
            OnDeathEvents(outcome);
        }

        void OnKillingDamage(Damage damage, bool silent = false) {
            if (!silent) {
                damage.Target?.PlayAudioClip(AliveAudioType.Die, true);
            }
            IsDead = true;
            if (ParentModel is Hero) {
                Log.Marking?.Warning("[Hero Death] Source: " + LogUtils.GetDebugName(damage.DamageDealer) + ", amount:" + damage.Amount + ", type: " + damage.Type + ", location: " + ParentModel.Coords);
            }
        }

        void HandleManaShield(Damage damage, out float damageToMana) {
            if (ParentModel is ICharacter iCharacter && iCharacter.CharacterStats.ManaShield.ModifiedValue > 0f) {
                LimitedStat mana = iCharacter.CharacterStats.Mana;
                float manaShieldPercentage = iCharacter.CharacterStats.ManaShield.ModifiedValue;
                float damageInterceptedByManaShield = damage.Amount * manaShieldPercentage;
                damageToMana = damageInterceptedByManaShield > mana.ModifiedValue ? mana.ModifiedValue : damageInterceptedByManaShield;
                
                mana.DecreaseBy(damageToMana);
                float damageToHealth = damage.Amount - damageToMana;
                if (damageToHealth <= 0) {
                    damage.RawData.SetToZero();
                } else {
                    damage.RawData.MultiplyMultModifier(damageToHealth / damage.Amount);
                }
            } else {
                damageToMana = 0f;
            }
        }

        void HandleManaShieldRetaliation(ICharacter damageReceiver, ICharacter damageDealer, float damageToMana) {
            if (damageToMana <= 0) return;
            
            float manaShieldRetaliationMultiplier = damageReceiver.CharacterStats.ManaShieldRetaliation.ModifiedValue;
            if (manaShieldRetaliationMultiplier > 0) {
                damageDealer.HealthElement.TakeDamage(new Damage(DamageParameters.ManaShieldRetaliation, damageReceiver, damageDealer, new RawDamageData(damageToMana * manaShieldRetaliationMultiplier)));
            }
        }
        
        void HandleMeleeRetaliation(ICharacter damageReceiver, ICharacter damageDealer, Item damagingItem, float damage) {
            if (damage <= 0) return;
            
            float meleeRetaliationMultiplier = damageReceiver.CharacterStats.MeleeRetaliation.ModifiedValue;
            if (meleeRetaliationMultiplier > 0 && damagingItem is {IsMelee: true}) {
                damageDealer.HealthElement.TakeDamage(new Damage(DamageParameters.MeleeRetaliation, damageReceiver, damageDealer, new RawDamageData(damage * meleeRetaliationMultiplier)));
            }
        }

        // === Event Senders
        public void TriggerVisualScriptingEvent(string eventName) {
            _eventMachines.ForEach(m => m.TriggerUnityEvent(eventName));
        }

        public void TriggerVisualScriptingOnDeath() {
            TriggerVisualScriptingEvent("OnDeath");
        }
        
        void OnDeathEvents(DamageOutcome outcome) {
            outcome.Damage.DamageDealer?.Trigger(Events.OnKill, outcome);

            if (outcome.Attacker is NpcElement npc && npc.IsSummon && npc.HasElement<NpcHeroSummon>()) {
                outcome.Damage.DamageDealer?.Trigger(Events.OnHeroSummonKill, outcome);
            }
            
            ParentModel.DieFromDamage(outcome);
        }

        void AfterHealthDecreaseEvents(DamageOutcome outcome) {
            var damage = outcome.Damage;
            ICharacter damageDealer = damage.DamageDealer;
            
            damageDealer?.Trigger(Events.OnDamageDealt, outcome);
            this.Trigger(Events.OnDamageTaken, outcome);
            TriggerVisualScriptingEvent("GetHit");
            
            if (outcome.DamageModifiersInfo.IsSneak) {
                damageDealer.Trigger(Events.OnSneakDamageDealt, outcome);
            }

            bool canApplyLifesteal = damageDealer is { HasBeenDiscarded: false } attacker && 
                                     attacker != damage.Target && attacker.CharacterStats.LifeSteal.ModifiedValue > 0f;
            if (canApplyLifesteal) {
                damageDealer.HealthElement.GetLifeFromLifesteal(outcome);
            }
        }

        void GetLifeFromLifesteal(DamageOutcome outcome) {
            if (ParentModel is ICharacter iCharacter) {
                float lifestealValue = outcome.FinalAmount * (iCharacter.CharacterStats.LifeSteal.ModifiedValue);
                iCharacter.Health.IncreaseBy(lifestealValue);
            }
        }

        void BeforeHealthDecreaseEvents(Damage damage) {
            damage.DamageDealer?.Trigger(Events.BeforeDamageDealt, damage);
            this.Trigger(Events.BeforeDamageTaken, damage);
        }
        
        DamageModifiersInfo ApplyDamageModifiers(Damage damage, out float dmgModifier) {
            // Only Hero can deal critical damage, and tool interactions never deal critical damage.
            if (damage.DamageDealer is not Hero hero || damage.Type == DamageType.Interact) {
                dmgModifier = 1f;
                return new DamageModifiersInfo(0,0,0,0);
            }

            ItemStats itemStats = damage.Item?.ItemStats;
            float critical = GetCriticalMultiplier(hero, damage, itemStats);
            float weakSpot = GetWeakSpotDamageMultiplier(hero, damage, itemStats);
            float sneak = GetSneakDamageMultiplier(hero, damage, itemStats);
            float backStab = GetBackStabDamageMultiplier(hero, damage);
            dmgModifier = 1 + critical + weakSpot + sneak + backStab;

            return new DamageModifiersInfo(critical, sneak, weakSpot, backStab);
        }

        public ref readonly HitboxData GetHitbox(Collider collider, out bool exists) {
            return ref _hitboxes.GetHitbox(collider, out exists);
        }
        
        public bool HasHitbox(Collider collider) {
            return _hitboxes.HasHitbox(collider);
        }

        // === VFX
        SurfaceType GetHitSurface(Damage damage, in HitboxData hitbox, bool hitboxExists) {
            if (damage.HitSurfaceType != null) {
                return damage.HitSurfaceType;
            }
            
            SurfaceType hitSurface = damage.Target?.AudioSurfaceType;
            if (hitboxExists && ParentModel is ICharacter ch) {
                foreach (var eqType in hitbox.ArmorType.EquipmentTypes) {
                    ItemAudio itemAudio = ch.Inventory.EquippedItem(eqType.MainSlotType)?.TryGetElement<ItemAudio>();
                    if (itemAudio?.AudioContainer.ArmorHitType is { } armorHitType) {
                        hitSurface = armorHitType;
                        break;
                    }
                }
            } else if (ParentModel is Hero h) {
                ItemAudio itemAudio = h.Inventory.EquippedItem(EquipmentSlotType.Cuirass)?.TryGetElement<ItemAudio>();
                if (itemAudio != null) {
                    hitSurface = itemAudio.AudioContainer.ArmorHitType;
                }
            }

            return hitSurface;
        }
        
        // === Spawning blood
        void SpawnOnHitEffect(Vector3 hitPosition, Vector3 hitDirection, Collider hitCollider) {
            if (ParentModel.HitVFX is { IsSet: true }) {
                VFXManager.SpawnCombatVFX(ParentModel.HitVFX, hitPosition, hitDirection, hitCollider).Forget();
            }
        }
        
        // === Audio
        static void HurtAudio(Damage damage, ref FragileSemaphore dotAudioSemaphore) {
            float healthTakenFactor = Mathf.Clamp01(damage.Amount / damage.Target.MaxHealth);
            FMODParameter parameter = new("Force", healthTakenFactor);
            
            if (!damage.IsDamageOverTime) {
                damage.Target?.PlayAudioClip(AliveAudioType.Hurt, true, parameter);
                dotAudioSemaphore.Set(true);
            } else if (dotAudioSemaphore.State) {
                if (damage.Target is Hero hero) {
                    var eventReference = CommonReferences.Get.AudioConfig.StatusAudioMap
                        .GetEventReference(damage.StatusDamageType, hero.GetGender());
                        
                    FMODManager.PlayOneShot(eventReference);
                } else {
                    damage.Target?.PlayAudioClip(AliveAudioType.Hurt, true, parameter);
                }

                dotAudioSemaphore.Set(true);
            }
        }
        
        static void HitAudio(Damage damage, SurfaceType hitSurface, DamageModifiersInfo dmgmModifiers, bool kill) {
            if (damage.Item == null || damage.Target == null || damage.IsDamageOverTime) {
                return;
            }
            ItemAudio itemAudio = damage.Item.TryGetElement<ItemAudio>();
            ItemAudioType itemAudioType = ItemAudioType.MeleeHit;
            
            if (itemAudio is { AudioContainer: { audioType: ItemAudioContainer.AudioType.Magic } }) {
                itemAudioType = ItemAudioType.MagicHit;
            } else if (damage.IsPush) {
                itemAudioType = ItemAudioType.PommelHit;
            } 
            
            EventReference eventReference = itemAudioType.RetrieveFrom(damage.Item);

            FMODParameter[] parameters = { 
                hitSurface, 
                new("Kill", kill), 
                new("CharacterHit", damage.Target is ICharacter),
                new("Critical", dmgmModifiers.IsCritical), 
                new("WeakSpot", dmgmModifiers.IsWeakSpot),
                new("Sneak", dmgmModifiers.IsSneak), 
                new("BackStab", dmgmModifiers.IsBackStab),
                new("Heavy", damage.IsHeavyAttack),
                new("Dash", damage.IsDashAttack),
                new("ShootingForce", damage.Parameters.BowDrawStrength)
            };

            damage.Item.PlayAudioClip(eventReference, true, parameters);
            
            if (damage.DamageDealer is Hero h) {
                EventReference heroHitEventReference = CommonReferences.Get.AudioConfig.HeroHitBonusAudio;
                h.PlayAudioClip(heroHitEventReference, true, parameters);
            }
        }
        
        
        // === Damage Multiplier
        DamageMultiplierResult AddDamageMultiplier(Vector3 position, Vector3 direction, float radius, Collider hitCollider, Item item) {
            // Add a multiplier if a particular collider was hit. Do not apply a multiplier if the damage is applied through a radius because multiple collider are hit.
            if (radius != 0 || direction == Vector3.zero || hitCollider == null) {
                return new DamageMultiplierResult { colliderHit = hitCollider, weakSpotMultiplier = 1 };
            }
            
            ref readonly var hitbox = ref _hitboxes.GetHitbox(hitCollider, out var hitboxExists);
            if (hitboxExists == false) {
                // The main collider may be overlapping child hitbox colliders. Perform one more raycast to ensure a hitbox collider shouldn't be hit.
                float distance = hitCollider switch {
                    CapsuleCollider capsuleCollider => capsuleCollider.radius,
                    SphereCollider sphereCollider => sphereCollider.radius,
                    _ => 0.2f
                };

                // The hitbox collider may be underneath the base collider. Fire a raycast to determine if there are any colliders underneath the hit collider 
                // that should apply a multiplier.
                var hitCount = Physics.RaycastNonAlloc(position, direction, _raycastHits, distance, LayerMask.GetMask("Hitboxes"), QueryTriggerInteraction.Ignore);
                for (int i = 0; i < hitCount; ++i) {
                    var closestRaycastHit = QuickSelect.SmallestK(_raycastHits, hitCount, i, _raycastHitComparer);
                    if (closestRaycastHit.collider == hitCollider) {
                        continue;
                    }

                    hitbox = ref _hitboxes.GetHitbox(closestRaycastHit.collider, out hitboxExists);
                    if (hitboxExists) {
                        break;
                    }
                }
            }
            return new DamageMultiplierResult { colliderHit = hitCollider, weakSpotMultiplier = hitbox.DamageMultiplier(item) };
        }

        static float GetCriticalMultiplier(Hero hero, Damage damage, ItemStats itemStats) {
            bool isCritical = damage.Parameters.CanBeCritical;
            isCritical = isCritical && (damage.Critical || CheckCriticalProbability(hero, damage));
            if (isCritical) {
                float multi = hero.HeroStats.CriticalDamageMultiplier;
                if (itemStats != null) {
                    multi += itemStats.CriticalDamageMultiplier;
                }
                return multi;
            }
            return 0;
        }

        static bool CheckCriticalProbability(Hero hero, Damage damage) {
            float totalChance = hero.HeroStats.CriticalChance + damage.AdditionalRandomnessData.AdditionalCritChance;
            if (damage.Item?.IsRanged ?? false) {
                totalChance += hero.HeroStats.RangedCriticalChance;
            } else {
                totalChance += damage.Type switch {
                    DamageType.MagicalHitSource => hero.HeroStats.MagicCriticalChance,
                    DamageType.PhysicalHitSource => hero.HeroStats.MeleeCriticalChance,
                    _ => 0
                };
            }

            return RandomUtil.WithProbability(totalChance * damage.AdditionalRandomnessData.RandomOccurrenceEfficiency);
        }

        static float GetWeakSpotDamageMultiplier(Hero hero, Damage damage, ItemStats itemStats) {
            if (damage.WeakSpotMultiplier > 1f) {
                bool isMelee = damage.Item?.IsMelee ?? false;
                float multi = isMelee ? hero.HeroStats.MeleeWeakSpotDamageMultiplier : hero.HeroStats.WeakSpotDamageMultiplier;
                if (itemStats != null) {
                    multi += itemStats.WeakSpotDamageMultiplier;
                }
                return (damage.WeakSpotMultiplier - 1f) * multi;
            }

            return 0;
        }

        float GetSneakDamageMultiplier(Hero hero, Damage damage, ItemStats itemStats) {
            if (!ParentModel.TryGetElement(out NpcAI npcAI)) {
                // Sneak Damage can only be applied to NPCs, barrels (etc.) can't see player.
                return 0;
            }

            bool isSneakDamage = !npcAI.InAlert && !npcAI.InCombat && !npcAI.IsRunningToSpawn && !npcAI.InWyrdConversion && !npcAI.InFlee;
            isSneakDamage = isSneakDamage && (npcAI.IsHostileTo(hero) || !npcAI.ParentModel.Element<NpcCrimeReactions>().IsSeeingHero);
            isSneakDamage = isSneakDamage && damage.IsPrimary;
            isSneakDamage = isSneakDamage && !damage.IsDamageOverTime;
            
            if (isSneakDamage) {
                float totalMultiplier = 0;
                if (damage.Item?.IsMelee ?? false) {
                    totalMultiplier += hero.HeroStats.MeleeSneakDamageMultiplier;
                }
                totalMultiplier += hero.HeroStats.SneakDamageMultiplier;
                if (itemStats != null) {
                    totalMultiplier += itemStats.SneakDamageMultiplier;
                }
                return totalMultiplier;
            }

            return 0;
        }
        
        static float GetBackStabDamageMultiplier(Hero hero, Damage damage) {
            if (damage.Parameters.IsBackStab) {
                float totalMultiplier = 0;
                totalMultiplier += damage.Item?.ItemStats?.BackStabDamageMultiplier ?? 0f;
                totalMultiplier += hero.HeroStats.BackStabDamageMultiplier;
                return totalMultiplier;
            }

            return 0;
        }

        struct DamageMultiplierResult {
            public Collider colliderHit;
            public float weakSpotMultiplier;
        }
    }
}
