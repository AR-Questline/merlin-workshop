using System;
using System.Runtime.CompilerServices;
using Awaken.TG.Main.AI.Fights.Projectiles;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Weapons;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Stats.Observers;
using Awaken.TG.Main.Heroes.Stats.Utils;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Locations.Spawners.Critters;
using Awaken.TG.Main.Locations.Views;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.Utility.LowLevel.Collections;
using JetBrains.Annotations;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Fights.DamageInfo {
    public class Damage {
        public ICharacter DamageDealer { get; private set; }
        public Item Item { get; private set; }
        public Item BlockingItem { [UnityEngine.Scripting.Preserve] get; private set; }
        public IAlive Target { get; private set; }
        public Skill Skill { get; private set; }
        public RawDamageData RawData { get; private set; }
        public RawRandomnessData AdditionalRandomnessData { get; private set; } = new();
        public RuntimeDamageTypeData DamageTypeData => Parameters.DamageTypeData;
        public RuntimeDamageReceivedMultiplierData DamageReceivedMultiplierData { get; private set; }
        public float StaminaDamageAmount { get; set; }
        public Collider HitCollider { get; set; }
        public SurfaceType HitSurfaceType { get; private set; }
        public DamageParameters Parameters { get; set; }
        public bool WeakSpotHit { get; set; }
        public float WeakSpotMultiplier { get; set; }
        public bool IsBlocked { get; private set; }
        public bool IsParried { get; private set; }
        public Projectile Projectile { get; set; }
        public float Amount => RawData.CalculatedValue;
        public DamageType Type => DamageTypeData.SourceType;
        public UnsafePinnableList<DamageTypeDataPart> SubTypes => DamageTypeData.Parts;
        public StatusDamageType StatusDamageType => Parameters.StatusDamageType;
        public SurfaceType DamageSurfaceType => Item?.DamageSurfaceType;
        public bool IsPrimary => Parameters.IsPrimary;
        public bool Inevitable => Parameters.Inevitable;
        public bool IgnoreArmor => Parameters.IgnoreArmor;
        public bool Critical => Parameters.Critical;
        public bool IsHeavyAttack => Parameters.IsHeavyAttack;
        public bool IsDashAttack => Parameters.IsDashAttack;
        public bool IsPush => Parameters.IsPush;
        public bool IsDamageOverTime => Parameters.IsDamageOverTime;

        public float Radius => Parameters.Radius;
        public float PoiseDamage => Parameters.PoiseDamage;
        public float ForceDamage => Parameters.ForceDamage;
        public float RagdollForce => Parameters.RagdollForce;
        public Vector3? Position => Parameters.Position ?? _position;
        public Vector3? Direction => Parameters.Direction ?? _direction;
        public Vector3? ForceDirection => Parameters.ForceDirection;
        [UnityEngine.Scripting.Preserve]  public Vector3 DealerPosition => 
            (Parameters.DealerPosition ?? (DamageDealer is Hero h ? h.CoordsOnNavMesh : DamageDealer?.Coords)) ?? Vector3.zero;
        
        public bool CanBeBlocked => !Inevitable && DamageDealer != null && !IsDamageOverTime && Type is DamageType.PhysicalHitSource or DamageType.MagicalHitSource;
        public bool CanBeReducedByArmor => !IgnoreArmor && (Damage.IsAnyPhysicalDamage(Type) || Type is DamageType.MagicalHitSource);

        Vector3? _position, _direction;

        public Damage(DamageParameters parameters, ICharacter dealer, IAlive target, RawDamageData data, float staminaDamageAmount = 0) {
            Parameters = parameters;
            DamageDealer = dealer;
            Target = target;
            DamageReceivedMultiplierData = target?.GetRuntimeDamageReceivedMultiplierData();
            RawData = data;
            StaminaDamageAmount = staminaDamageAmount;
        }
        
        // === Construction
        public Damage WithHitCollider(Collider hitCollider) {
            HitCollider = hitCollider;
            return this;
        }

        [UnityEngine.Scripting.Preserve]
        public Damage WithSkill(Skill skill) {
            Skill = skill;
            return this;
        }

        public Damage WithItem(Item item) {
            if (item is {HasBeenDiscarded : true}) {
                Log.Important?.Error($"Trying to use item that has been already discarded! {item}");
                return this;
            }
            
            Item = item;
            if (item is { ItemStats: not null }) {
                DamageParameters parameters = Parameters;
                parameters.ArmorPenetration += item.ItemStats.ArmorPenetration;
                Parameters = parameters;
                AdditionalRandomnessData.SetRandomOccurrenceEfficiency(item.ItemStats.RandomOccurrenceEfficiency);
            }

            return this;
        }
        
        public Damage WithStatusDamageType(StatusDamageType status) {
            var parameters = Parameters;
            parameters.StatusDamageType = status;
            Parameters = parameters;
            return this;
        }
        
        public Damage WithOverridenRandomOccurrenceEfficiency(float overridenRandomOccurrenceEfficiency) {
            AdditionalRandomnessData.SetRandomOccurrenceEfficiency(overridenRandomOccurrenceEfficiency);
            return this;
        }

        public Damage WithProjectile(Projectile projectile) {
            Projectile = projectile;
            return this;
        }

        public Damage WithPosition(Vector3 position) {
            _position = position;
            return this;
        }

        public Damage WithDirection(Vector3 direction) {
            _direction = direction;
            return this;
        }

        public Damage WithStaminaDamage(float amount) {
            StaminaDamageAmount = amount;
            return this;
        }

        public Damage WithHitSurface(SurfaceType surfaceType) {
            HitSurfaceType = surfaceType;
            return this;
        }
        
        // === Operations
        public void SetBlocked(Item blockingItem) {
            IsBlocked = true;
            IsParried = false;
            BlockingItem = blockingItem;
        }
        
        public void SetParried(Item parringItem) {
            IsBlocked = true;
            IsParried = true;
            BlockingItem = parringItem;
        }

        // === Utils
        
        public void ApplyBeforeDamageMultipliedModifiers() {
            DamageDealer?.Trigger(HealthElement.Events.BeforeDamageMultiplied, this);
            (Target as ICharacter)?.Trigger(HealthElement.Events.BeforeDamageTakenMultiplied, this);
        }

        public void ApplyOnDamageMultipliedModifiers(DamageModifiersInfo modifiersInfo) {
            var modifiedDamageInfo = new ModifiedDamageInfo(this, modifiersInfo);
            DamageDealer?.Trigger(HealthElement.Events.OnDamageMultiplied, modifiedDamageInfo);
            (Target as ICharacter)?.Trigger(HealthElement.Events.OnDamageTakenMultiplied, modifiedDamageInfo);
        }

        [UnityEngine.Scripting.Preserve]
        public static float PreCalculateTakenDamage(ICharacter target, float damage, bool ignoreArmor, DamageSubType damageSubType = DamageSubType.GenericPhysical) {
            if (target == null || target.WasDiscarded || !target.HasElement<CharacterStats>()) {
                return Mathf.Max(damage, 1f);
            }

            // apply armor
            var armor = target.TotalArmor(damageSubType);
            if (!ignoreArmor || armor < 0f) {
                damage *= GetArmorMitigatedMultiplier(armor);
            }

            // apply incoming damage stat
            float damageModifier = target.CharacterStats.IncomingDamage;
            damage *= damageModifier;
            // damage must by at least 1
            damage = Mathf.Max(damage, 1f);

            return damage;
        }

        // === Damage calculation based on equipped weapon
        static FloatRange CalcMinMaxDmg(ICharacter owner, ItemStats dmgSource, FloatRange minMax) {
            GetStatModifiers(owner, dmgSource, out float multStatModifier, out float linearStatModifier);
            
            float minDmg, maxDmg;
            if (owner is NpcElement) {
                GetDmgSourceDamage(owner, dmgSource, out float damageValue);
                minDmg = (damageValue + linearStatModifier) * multStatModifier;
                maxDmg = (damageValue + linearStatModifier) * multStatModifier;
            } else {
                minDmg = (minMax.min + linearStatModifier) * multStatModifier;
                maxDmg = (minMax.max + linearStatModifier) * multStatModifier;
            }

            return new FloatRange(minDmg, maxDmg);
        }
        
        public static void GetStatModifiers(ICharacter owner, ItemStats dmgSource, out float multStatModifier, out float linearStatModifier) {
            // Retrieve proficiency
            ProfAbstractRefs profRefs = ProfUtils.GetProfAbstractRefs(dmgSource.ProfFromAbstract);
            ProfStatType proficiency = profRefs.Proficiency;
            
            // Calculate multi stat modifier
            Stat multiStat = null;
            Stat strength = null;

            if (proficiency != null) {
                multiStat = owner.Stat(proficiency.MultiStat);
                strength = proficiency.UseStrength ? owner.Stat(CharacterStatType.Strength) : null;
            }

            if (strength == null && multiStat == null) {
                multStatModifier = 1;
            } else {
                // Use compound stat to apply tweaks of both stats to sum of base values
                CompoundStat compound = new(true, strength, multiStat);
                multStatModifier = compound.ModifiedValue;
                
                bool isRanged = dmgSource.ParentModel?.IsRanged ?? false;
                bool isThrowable = dmgSource.ParentModel?.IsThrowable ?? false;
                if (isRanged || isThrowable) {
                    multStatModifier += owner.Stat(CharacterStatType.RangedDamageMultiplier) - 1;
                } else if (dmgSource.ParentModel?.IsMelee ?? false) {
                    multStatModifier += owner.Stat(CharacterStatType.MeleeDamageMultiplier) - 1;
                }
            }
            
            // Calculate linear stat modifier
            if (proficiency?.UseStrength ?? false) {
                linearStatModifier = owner.Stat(CharacterStatType.StrengthLinear)?.ModifiedValue ?? 0f;
            } else {
                linearStatModifier = 0f;
            }
            
            // Calculate requirements
            multStatModifier *= ItemRequirementsUtils.GetDamageMultiplier(owner, dmgSource.ParentModel);
            
            // Npcs are handled differently
            if (owner is NpcElement) {
                GetNpcStatModifiers(dmgSource, out var npcMultStatModifier, out var npcLinearStatModifier);
                multStatModifier *= npcMultStatModifier;
                linearStatModifier += npcLinearStatModifier;
            }
        }

        static void GetNpcStatModifiers(ItemStats dmgSource, out float multStatModifier, out float linearStatModifier) {
            linearStatModifier = 0;
            multStatModifier = dmgSource.NpcDamageMultiplier;
        }

        public static void GetDmgSourceDamage(ICharacter owner, ItemStats dmgSource, out float damageValue) {
            if (owner is NpcElement npc && dmgSource.ParentModel is {} item) {
                var correctStat = item.IsMagic ? NpcStatType.MagicDamage : item.IsRanged ? NpcStatType.RangedDamage : NpcStatType.MeleeDamage;
                damageValue = npc.Stat(correctStat).ModifiedValue;
            } else {
                damageValue = dmgSource.DamageValue.ModifiedValue;
            }
        }

        public static void GetNpcDmgBasedOnDamageType(NpcElement npc, DamageType type, bool isRanged, out float damageValue) {
            NpcStatType correctStat;
            if (type == DamageType.MagicalHitSource) {
                correctStat = NpcStatType.MagicDamage;
            } else if (isRanged) {
                correctStat = NpcStatType.RangedDamage;
            } else {
                correctStat = NpcStatType.MeleeDamage;
            }
            damageValue = npc.Stat(correctStat).ModifiedValue;
        } 

        /// <summary>
        /// Get the MinMax damage value that the character will deal with equipment in slot
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        public static FloatRange PreCalculateDealtDamage(ICharacter source, EquipmentSlotType typeToCheck) {
            Item itemInSlot = source?.Inventory.EquippedItem(typeToCheck);
            return PreCalculateDealtDamage(source, itemInSlot);
        }
        
        /// <summary>
        /// Get the MinMax damage value that the character will deal with provided weapon
        /// </summary>
        public static FloatRange PreCalculateDealtDamage(ICharacter owner, Item weapon) {
            if (owner == null || owner.WasDiscarded || weapon?.ItemStats == null) {
                return new FloatRange(0,0);
            }

            ItemStats dmgSource = weapon.ItemStats;
            
            return CalcMinMaxDmg(owner, dmgSource, new FloatRange(dmgSource.BaseMinDmg, dmgSource.BaseMaxDmg));
        }

        /// <summary>
        /// Get Damage that the target will deal with its weapon
        /// </summary>
        /// <exception cref="NullReferenceException">Improperly prepared parameters for damage calculation</exception>
        public static Damage CalculateDamageDealt(ICharacter dealer, IAlive receiver, DamageParameters dmgParams, Item sourceWeapon) {
            if (dealer == null || dealer.WasDiscarded || receiver == null || receiver.WasDiscarded) {
                throw new NullReferenceException("Improperly prepared parameters for damage calculation");
            }
            
            ItemStats dmgSource = sourceWeapon.ItemStats;
            GetStatModifiers(dealer, dmgSource, out float multStatModifier, out float linearStatModifier);
            GetDmgSourceDamage(dealer, dmgSource, out float damageValue);
            var rawDamageData = new RawDamageData(damageValue, multStatModifier, linearStatModifier);

            dmgParams.DamageTypeData = dmgSource.RuntimeDamageTypeData; 

            return new Damage(dmgParams, dealer, receiver, rawDamageData).WithItem(sourceWeapon);
        }
        /// <summary>
        /// Get Damage that the target will deal with its weapon. Draw strength should usually be 0-1 (minDmg-maxDmg)
        /// </summary>
        public static RawDamageData GetThrowableDamageDamageFrom(ICharacter dealer, Item sourceWeapon, float drawStrength) {
            if (dealer == null || dealer.HasBeenDiscarded || sourceWeapon == null || sourceWeapon.HasBeenDiscarded) {
                throw new NullReferenceException("Improperly prepared parameters for damage calculation");
            }
            ItemStats dmgSource = sourceWeapon.ItemStats;
            GetStatModifiers(dealer, dmgSource, out float multStatModifier, out float linearStatModifier);
            GetDmgSourceDamage(dealer, dmgSource, out float damageValue);

            // For now just use random value between min/max since there is no proficiency or stats for throwAbles.
            return new RawDamageData(damageValue, multStatModifier, linearStatModifier);
        }
        
        /// <summary>
        /// Get Damage that the target will deal with its weapon. Draw strength should usually be 0-1 (minDmg-maxDmg)
        /// </summary>
        public static RawDamageData GetBowDamageFrom(ICharacter dealer, Item sourceWeapon, float drawStrength, Item sourceProjectile) {
            if (dealer == null || dealer.HasBeenDiscarded || sourceWeapon == null || sourceWeapon.HasBeenDiscarded) {
                throw new NullReferenceException("Improperly prepared parameters for damage calculation");
            }
            
            ItemStats dmgSource = sourceWeapon.ItemStats;

            GetStatModifiers(dealer, dmgSource, out float multStatModifier, out float linearStatModifier);
            GetDmgSourceDamage(dealer, dmgSource, out float bowDamage);
            
            float arrowDamage = sourceProjectile?.ItemStats?.DamageValue.ModifiedValue ?? 0;
            float totalDamage = (bowDamage + arrowDamage) * drawStrength;
            
            RawDamageData rawDamageData = new(totalDamage, multStatModifier, linearStatModifier);
            return rawDamageData;
        }

        /// <summary>
        /// Get Damage that the target will deal with its spell.
        /// </summary>
        public static RawDamageData GetMagicProjectileDamageFrom(ICharacter dealer, Item sourceWeapon, float chargeAmount) {
            if (dealer == null || dealer.HasBeenDiscarded || sourceWeapon == null || sourceWeapon.HasBeenDiscarded) {
                throw new NullReferenceException("Improperly prepared parameters for damage calculation");
            }
            
            ItemStats dmgSource = sourceWeapon.ItemStats;

            GetStatModifiers(dealer, dmgSource, out float multStatModifier, out float linearStatModifier);
            GetDmgSourceDamage(dealer, dmgSource, out float damageValue);
            
            float projectileDamage = damageValue * chargeAmount;
            // Currently unused
            //projectileDamage += dmgSource.DamageIncreasePerCharge.ModifiedValue * chargeAmount;
            
            RawDamageData rawDamageData = new(projectileDamage, multStatModifier, linearStatModifier);
            return rawDamageData;
        }
        
        [UnityEngine.Scripting.Preserve]
        public static RawDamageData GetBowDamageFrom(ICharacter dealer, float drawStrength, EquipmentSlotType typeToCheck) {
            Item dmgSource = dealer.Inventory.EquippedItem(typeToCheck);
            Item sourceProjectile = dealer.Inventory.EquippedItem(EquipmentSlotType.Quiver);
            return GetBowDamageFrom(dealer, dmgSource, drawStrength, sourceProjectile);
        }

        [UnityEngine.Scripting.Preserve]
        public static float GetDamageValueFromItemSimple(ICharacter dealer, Item item) {
            ItemStats dmgSource = item.ItemStats;
            GetStatModifiers(dealer, dmgSource, out float multStatModifier, out float linearStatModifier);
            GetDmgSourceDamage(dealer, dmgSource, out float damageValue);
            return (damageValue + linearStatModifier) * multStatModifier;
        }

        /// <summary>
        /// Get the damage that the target will deal with its weapon
        /// </summary>
        public static RawDamageData GetDamageFrom(ICharacter dealer, EquipmentSlotType typeToCheck) {
            ItemStats dmgSource = dealer.Inventory.EquippedItem(typeToCheck).ItemStats;
            if (dmgSource == null) return new RawDamageData(0);

            GetStatModifiers(dealer, dmgSource, out float multStatModifier, out float linearStatModifier);
            GetDmgSourceDamage(dealer, dmgSource, out float damageValue);
            
            RawDamageData rawDamageData = new(damageValue, multStatModifier, linearStatModifier);
            return rawDamageData;
        }
        
        /// <summary>
        /// Get the damage that the target will deal with its weapon
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        public static RawDamageData GetDmgForBehaviour(ICharacter dealer, EquipmentSlotType typeToCheck, DamageType damageType, bool IsRanged) {
            if (dealer is not NpcElement npc) {
                // If not npc, we should handle it normally
                return GetDamageFrom(dealer, typeToCheck);
            }

            float multStatModifier, linearStatModifier;
            if (typeToCheck != null && dealer.Inventory.EquippedItem(typeToCheck)?.ItemStats is {} dmgSource) {
                GetNpcStatModifiers(dmgSource, out multStatModifier, out linearStatModifier);
            } else {
                multStatModifier = 1;
                linearStatModifier = 0;
            }
            
            GetNpcDmgBasedOnDamageType(npc, damageType, IsRanged, out float damageValue);
            
            RawDamageData rawDamageData = new(damageValue, multStatModifier, linearStatModifier);
            return rawDamageData;
        }

        // === Misc
        [UnityEngine.Scripting.Preserve]
        public static int TakeStatDamage(CharacterStatType stat, ICharacter character, float damage) {
            int damageInt = (int) damage;
            stat.RetrieveFrom(character).DecreaseBy(damageInt);
            return damageInt;
        }

        // === Targets
        public static VLocation DetermineTargetHit(Collider colliderHit, out IAlive receiver, out HealthElement healthElement) {
            var damageable = colliderHit.GetComponentInParent<IDamageable>();
            receiver = null;
            healthElement = null;
            if (damageable is Critter critter) {
                critter.OnAttacked();
                return null;
            }

            VLocation location = damageable as VLocation;
            if (location != null && location.HasBeenDiscarded) {
                return null;
            }
            
            receiver = location?.GenericTarget?.TryGetElement<IAlive>();
            try {
                receiver ??= VGUtils.GetModel<IAlive>(colliderHit.gameObject);
                if (receiver?.HasBeenDiscarded ?? true) receiver = null;
                healthElement = receiver?.HealthElement;
            } catch {
                receiver = null;
                healthElement = null;
            }

            return location;
        }

        public static float GetArmorMitigatedMultiplier(float armorValue) {
            float reduction = GetArmorDamageReduction(armorValue);
            return 1 - reduction;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetArmorDamageReduction(float armorValue) {
            float reduction = armorValue / 100f;
            reduction = Mathf.Clamp(reduction, 0, 0.9999f);
            return reduction;
        }

        public static bool IsAnyPhysicalDamage(DamageType type) 
            => type is DamageType.Fall or DamageType.PhysicalHitSource or DamageType.Trap or DamageType.Environment;

        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        public void AddBonusMultiplier(float bonus) {
            RawData.AddMultModifier(bonus);
        }
        
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        public void AddBonusLinear(float bonus) {
            RawData.AddLinearModifier(bonus);
        }
        
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        public void SetToZero() {
            RawData.SetToZero();
        }
    }
}