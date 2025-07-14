using System;
using System.Collections.Generic;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.AI.SummonsAndAllies;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Crafting;
using Awaken.TG.Main.Crafting.AlchemyCrafting;
using Awaken.TG.Main.Crafting.Cooking;
using Awaken.TG.Main.Crafting.HandCrafting;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Stats.Controls;
using Awaken.TG.Main.Heroes.Stats.Utils;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Actions.Lockpicking;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using JetBrains.Annotations;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Heroes.Stats.Observers {
    public partial class ProficiencyEventListener : Element<Hero> {
        static readonly string ProficiencyPrefix = $"{"[Debug]".ColoredText(Log.Utils.PrefixColor)}  {"Proficiency > ".ColoredText(ARColor.EditorBlue)}";
        static bool DebugMode => SafeEditorPrefs.GetBool("debug.proficiency");

        public sealed override bool IsNotSaved => true;

        readonly List<ProficiencyParams> _profParams = new();
        readonly List<ProfAbstractRefs> _profReferences = new();
        
        const int SneakXPInterval = 1;
        const int SprintXPInterval = 1;
        float _lastSprintXPUpdateTime;
        float _lastSneakXPUpdateTime;
        RecurringActions _recurringActions;
        [CanBeNull] ProfStatType _currentArmorProf;
        
        Hero Hero => ParentModel;
        RecurringActions RecurringActions => _recurringActions ??= World.Services.Get<RecurringActions>();

        public new static class Events {
            [UnityEngine.Scripting.Preserve]
            public static readonly Event<ProficiencyEventListener, Dictionary<ProfStatType, float>> EquipmentTypePercentagesChanged =
                new(nameof(EquipmentTypePercentagesChanged));
        }

        protected override void OnInitialize() {
            ParentModel.AfterFullyInitialized(AfterHeroFullyInitialized);
        }
        
        void AfterHeroFullyInitialized() {
            World.EventSystem.ListenTo(EventSelector.AnySource, HealthElement.Events.OnDamageTaken, this, OnDamageTakenCallback);
            ParentModel.Element<ArmorWeight>().ListenTo(ArmorWeight.Events.ArmorWeightScoreChanged, ArmorWeightScoreChangeCallback, this);

            ParentModel.ListenTo(Hero.Events.HeroSprintingStateChanged, SprintStateChangeCallback, this);
            ParentModel.ListenTo(Hero.Events.HeroJumped, HeroJumpedCallback, this);
            ParentModel.ListenTo(Hero.Events.HeroDashed, HeroDashedCallback, this);
            ParentModel.ListenTo(Hero.Events.HeroSlid, HeroSlidCallback, this);
            ParentModel.ListenTo(Hero.Events.SneakingStateChanged, SneakingStateChangeCallback, this);
            ParentModel.ListenTo(HealthElement.Events.OnSneakDamageDealt, SneakDamageDealtCallback, this);
            ParentModel.ListenTo(Hero.Events.HeroBlockedDamage, HeroBlockedDamageCallback, this);
            ParentModel.ListenTo(Hero.Events.HeroParriedDamage, HeroParriedDamageCallback, this);
            ParentModel.ListenTo(CommitCrime.Events.PickpocketSuccess, HeroPickpocketSuccess, this);
            ParentModel.ListenTo(CommitCrime.Events.PickpocketFail, HeroPickpocketFail, this);
            ParentModel.ListenTo(LockpickingInteraction.Events.HeroLockUnlocked, HeroLockUnlocked, this);
            ParentModel.ListenTo(LockpickingInteraction.Events.HeroPickBroke, HeroPickBroke, this);
            ParentModel.ListenTo(INpcSummon.Events.SummonSpawned, OnHeroSummonSpawnedCallback, this);
            ParentModel.ListenTo(Crafting.Crafting.Events.Created, CraftingCreatedItem, this);
            
            _profParams.AddRange(ProfUtils.ProfParams());
            _profParams.ForEach(p => p.AttachListeners(Hero.Current, this));
            _profReferences.AddRange(ProfUtils.ProfReferences());

            ArmorWeightScoreChangeCallback(-1);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            RecurringActions.UnregisterAction($"{this.ID}_Sprint");
            RecurringActions.UnregisterAction($"{this.ID}_Sneak");
        }

        // === Callbacks

        void HeroJumpedCallback() => XPGainEvent(ProfStatType.Acrobatics, BaseXPType.Jump, 1);
        void HeroDashedCallback() => XPGainEvent(ProfStatType.Evasion, BaseXPType.Dash, 1);
        void HeroSlidCallback() => XPGainEvent(ProfStatType.Evasion, BaseXPType.Slide, 1);
        void HeroBlockedDamageCallback(float dmgBlocked) => XPGainEvent(ProfStatType.Shield, BaseXPType.DmgBlocked, dmgBlocked);
        void HeroParriedDamageCallback(float dmgParried) => XPGainEvent(ProfStatType.Shield, BaseXPType.DmgBlocked, dmgParried);

        void HeroPickpocketSuccess(Item item) => XPGainEvent(ProfStatType.Theft, BaseXPType.PickpocketSuccess, item.Price * item.Quantity);
        void HeroPickpocketFail(Item item) => XPGainEvent(ProfStatType.Theft, BaseXPType.PickpocketFail, item.Price * item.Quantity);
        void HeroPickBroke(LockAction obj) => XPGainEvent(ProfStatType.Theft, BaseXPType.PickBroke, 1);
        void HeroLockUnlocked(LockAction obj) => XPGainEvent(ProfStatType.Theft, BaseXPType.LockUnlocked, obj.Tolerance.xpMultiplier);
        void OnSummonDamageDealt(DamageOutcome dmgOutcome) {
            if (dmgOutcome.Damage.Type == DamageType.Fall) return;
            XPGainEvent(ProfStatType.Magic, BaseXPType.SummonDmgDealt, dmgOutcome.FinalAmount);
        }

        void OnHeroSummonSpawnedCallback(INpcSummon summon) {
            summon.ParentModel.ListenTo(HealthElement.Events.OnDamageDealt, OnSummonDamageDealt, this);
        }
        
        void CraftingCreatedItem(CreatedEvent args) {
            float xpGainParameter = args.Item.ExactPrice * args.Item.Quantity;
            switch (args.CraftingTemplate) {
                case AlchemyTemplate: XPGainEvent(ProfStatType.Alchemy, BaseXPType.Alchemy, xpGainParameter); break;
                case CookingTemplate: XPGainEvent(ProfStatType.Cooking, BaseXPType.Cooking, xpGainParameter); break;
                case HandcraftingTemplate: XPGainEvent(ProfStatType.Handcrafting, BaseXPType.Handcrafting, xpGainParameter); break;
                default: Log.Important?.Error("Unknown crafting template"); break;
            }
        }

        /// <summary>
        /// Looks for damage dealt by player to anything other than self or damage received by player. Registers as XP
        /// </summary>
        /// <param name="dmgOutcome">Damage info</param>
        void OnDamageTakenCallback(DamageOutcome dmgOutcome) {
            Damage damage = dmgOutcome.Damage;
#if UNITY_EDITOR
            if (DebugMode && dmgOutcome.FinalAmount < 999999) {
                foreach (var subType in dmgOutcome.Damage.SubTypes) {
                    float armor = dmgOutcome.Target.HasBeenDiscarded ? -1 : dmgOutcome.Target.TotalArmor(subType.SubType);
                    Log.Minor?.Info(
                        $"{ProficiencyPrefix}DamageOutcomeTarget: {damage.Target} damageDealer: {dmgOutcome.Attacker} Armor Hit: {armor} Damage dealt: {dmgOutcome.FinalAmount} DamageType: {damage.Type}",
                        dmgOutcome.HitCollider?.gameObject,
                        LogOption.NoStacktrace);
                }
            }
#endif
            
            if (dmgOutcome.Attacker == ParentModel) { //Attacker is hero
                if (dmgOutcome.Attacker != damage.Target) {
                    //Receiver is not hero / Attacker is not receiver
                    var heldItem = damage.Item;
                    if (heldItem == null || heldItem.IsThrowable || damage.Type == DamageType.Status || !damage.IsPrimary) return;
                
                    XPGainEvent(
                        ProfUtils.ProfFromAbstracts(heldItem, _profReferences), 
                        BaseXPType.DmgDealt, 
                        dmgOutcome.FinalAmount);
                }
            }
            // Not else to allow for any source of fall damage against player to register
            if (damage.Target == ParentModel) {
                // Attacker is not hero, but hero is receiver
                if (damage.Type == DamageType.Fall) {
                    XPGainEvent(ProfStatType.Acrobatics, BaseXPType.FallDmg, dmgOutcome.FinalAmount);
                    return;
                }

                if (dmgOutcome.Attacker == ParentModel) return;
                
                if (damage.CanBeReducedByArmor && _currentArmorProf != null) {
                    XPGainEvent(_currentArmorProf, BaseXPType.DmgReceived, damage.Amount);
                }
            }
        }

        void SprintStateChangeCallback(bool newState) {
            if (newState) {
                _lastSprintXPUpdateTime = Time.time; // For case if sprint lasted < 1 interval
                RecurringActions.RegisterAction(SprintXPAdd, $"{this.ID}_Sprint", SprintXPInterval);
            } else {
                RecurringActions.UnregisterAction($"{this.ID}_Sprint");
                
                //Calculate the missing XP gained from last update
                float currentTime = Time.time;
                SprintXPAdd(currentTime - _lastSprintXPUpdateTime);
                _lastSprintXPUpdateTime = currentTime;
            }
        }

        void SneakDamageDealtCallback(DamageOutcome outcome) {
            var baseXpType = outcome.Damage.Item is { IsMelee: true } ? BaseXPType.DmgDealt : BaseXPType.RangedDmgDealt;
            XPGainEvent(ProfStatType.Sneak, baseXpType, 1);
        }

        void SneakingStateChangeCallback(bool newState) {
            if (newState) {
                _lastSneakXPUpdateTime = Time.time; // For case if sneak lasted < 1 interval
                RecurringActions.RegisterAction(SneakXPAdd, $"{this.ID}_Sneak", SneakXPInterval);
            } else {
                RecurringActions.UnregisterAction($"{this.ID}_Sneak");

                //Calculate the missing XP gained from last update
                float currentTime = Time.time;
                SneakXPAdd(currentTime - _lastSneakXPUpdateTime);
                _lastSneakXPUpdateTime = currentTime;
            }
        }

        /// <summary>
        /// Updates current armour proficiency
        /// </summary>
        void ArmorWeightScoreChangeCallback(float _) {
            ItemWeight weight = Hero.TryGetElement<ArmorWeight>()?.ArmorWeightType;

            if (weight == ItemWeight.Light) _currentArmorProf = ProfStatType.LightArmor;
            else if (weight == ItemWeight.Medium) _currentArmorProf = ProfStatType.MediumArmor;
            else if (weight == ItemWeight.Heavy) _currentArmorProf = ProfStatType.HeavyArmor;
            else if (weight == ItemWeight.Overload) _currentArmorProf = null;
            else {
                Log.Important?.Error("No Proficiency found for ItemWeight: " + weight);
            }
        }
        
        // === General proficiency gain event handler
        
        /// <summary>
        /// Calculate and apply Proficiency XP event
        /// </summary>
        /// <param name="proficiencyToLevel">The proficiency that should be leveled</param>
        /// <param name="targetXPType">the source of the event</param>
        /// <param name="xpGainParameter">additional parameter for baseXP to allow for scaled quantities</param>
        void XPGainEvent(ProfStatType proficiencyToLevel, BaseXPType targetXPType, float xpGainParameter) {
            if (World.Any<ProficiencyGainBlockerModel>()) return;

            float xpGain = CalculateXPGain(
                targetXPType,
                _profParams.Find(x => x.proficiency == proficiencyToLevel),
                xpGainParameter);

#if UNITY_EDITOR
            if (DebugMode) {
                // replace following with call to Log.Minor?.Info
                Log.Minor?.Info(
                    $"{ProficiencyPrefix}{proficiencyToLevel.EnumName} proficiency XP gained: {xpGain}\nfrom {targetXPType.EnumName}",
                    logOption: LogOption.NoStacktrace);
            }
#endif
            ParentModel.ProficiencyStats.TryAddXP(proficiencyToLevel, xpGain);
        }
        
        
        // === Callback Helpers

        /// <summary>
        /// Running xp add from recurring event
        /// </summary>
        void SprintXPAdd() => SprintXPAdd(SprintXPInterval);

        void SprintXPAdd(float time) {
            _lastSprintXPUpdateTime = Time.time;

            XPGainEvent(
                ProfStatType.Athletics, 
                Hero.IsSwimming 
                    ? BaseXPType.FastSwim 
                    : BaseXPType.Sprint, 
                time);
        }

        /// <summary>
        /// Running xp add from recurring event
        /// </summary>
        void SneakXPAdd() => SneakXPAdd(SneakXPInterval);

        void SneakXPAdd(float time) {
            var baseXpType = AreEnemiesNearby() ? BaseXPType.Sneak : BaseXPType.Walk;
            _lastSneakXPUpdateTime = Time.time;
            XPGainEvent(
                ProfStatType.Sneak,
                baseXpType,
                time);
        }
        
        bool AreEnemiesNearby() {
            var radius = GameConstants.Get.sneakNearbyEnemiesRadius;
            foreach (var npc in World.Services.Get<NpcGrid>().GetNpcsInSphere(Hero.Coords, radius)) {
                if (npc.AntagonismTo(Hero) == Antagonism.Hostile) {
                    return true;
                }
            }
            return false;
        }

        // === Skill XP calculation helpers
        
        //Skill Use Mult * (base XP * skill specific multipliers) + Skill Use Offset
        static float CalculateXPGain(BaseXPType baseXPType, ProficiencyParams fnParams, float additionalParam = 1) 
            => fnParams.skillUseMult * (GetBaseXP(baseXPType, fnParams, additionalParam) * fnParams.skillSpecificMult) + fnParams.skillUseOffset;

        static float GetBaseXP(BaseXPType baseXPType, ProficiencyParams functionParameters, float additionalParam) =>
            baseXPType.ApplyTypeSpecificFunction(functionParameters.GetBaseXP(baseXPType), additionalParam);
    }

    [Serializable]
    public struct ProfAbstractRefs {
        [RichEnumExtends(typeof(ProfStatType))][Space(2)]
        public RichEnumReference proficiency;
        [TemplateType(typeof(ItemTemplate)), SerializeField]
        TemplateReference abstractTemplate;
        
        public ItemTemplate AbstractTemplate => abstractTemplate?.Get<ItemTemplate>();
        public ProfStatType Proficiency => proficiency?.EnumAs<ProfStatType>();
    }
}
