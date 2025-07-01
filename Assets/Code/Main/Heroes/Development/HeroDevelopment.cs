using System;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Development.WyrdPowers;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.Stories.Quests.Objectives;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.Exp;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.HeroLevelUp;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.WyrdInfo;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes.Development {
    /// <summary>
    /// Element responsible for all aspects of hero development, like leveling, upgrades, learning new skills.
    /// </summary>
    public partial class HeroDevelopment : Element<Hero> {
        public override ushort TypeForSerialization => SavedModels.HeroDevelopment;

        // === WyrdWhispers
        [Saved] StructList<string> _wyrdWhisperFlagsCollected;
        string[] WyrdWhisperFlags => GameConstants.Get.wyrdWhisperFlags;

        // === Talents
        [Saved] public bool CanUseHeavyAttack { get; private set; }
        [Saved] public bool CanZoomBow { get; private set; }
        [Saved] public bool CanDash { get; private set; }
        [Saved] public bool CanSlide { get; private set; }
        [Saved] public bool CanSprintAttack { get; private set; }
        [Saved] public bool CanPommel { get; private set; }
        [Saved] public bool CanParry { get; private set; }
        [Saved] public bool SpectralWeaponsPenetrateShields { get; private set; }
        [Saved] public bool HeavyArmorNoStaminaUsageIncrease { get; private set; }
        [Saved] public bool ArmorReducedManaUsage { get; private set; }
        [Saved] public bool DontDealDamageToSummons { get; private set; }
        [Saved] public bool CanGatherAdditionalPlants { get; private set; }
        [Saved] public bool CanDashWhileAiming { get; private set; }
        [Saved] public bool CanParryDeflectProjectiles { get; private set; }
        [Saved] public bool ParryDeflectionTargetsEnemies { get; private set; }
        [Saved] public bool IncreasedFishingRodsDurability { get; private set; }
        [Saved] public bool ConsumeLessAlcoholInAlchemy { get; private set; }
        [Saved] public bool RemoveNegativeLevelsForCraftingItems { get; private set; }
        [Saved] public bool CheaperBonfireUpgrade { get; private set; }
        [Saved] public int BonfireCraftingLevel { get; private set; }

        // === Properties
        public WyrdSoulFragments WyrdSoulFragments => Element<WyrdSoulFragments>();
        public int WyrdSoulFragmentsCount => WyrdSoulFragments.UnlockedFragmentsCount;

        [UnityEngine.Scripting.Preserve]
        public WyrdSkillActivation WyrdSkillActivation => Element<WyrdSkillActivation>();

        public Stat Level => Hero.CharacterStats.Level;
        public LimitedStat XP => Hero.HeroStats.XP;
        public Stat WyrdWhispers => Hero.HeroStats.WyrdWhispers;
        [UnityEngine.Scripting.Preserve] public Stat WyrdMemoryShards => Hero.HeroStats.WyrdMemoryShards;
        public Stat TalentPoints => Hero.CharacterStats.TalentPoints;
        public Stat BaseStatPoints => Hero.CharacterStats.BaseStatPoints;
        
        Hero Hero => ParentModel;
        Stat XPForNextLevel => Hero.HeroStats.XPForNextLevel;

        bool _levelUpInProgress;
        
        // === Static
        public static int RequiredExpFor(int level) {
            var levelSchema = CommonReferences.Get.HeroExpPerLevelSchema;
            return level - 2 < levelSchema.exp.Length
                ? levelSchema.exp[level - 2]
                : (int)(level * levelSchema.a + levelSchema.b);
        }

        public static int RoundExp(float exp) {
            int rounded = (int)exp;
            if (rounded > 1000) {
                rounded = rounded / 100 * 100;
            } else if (rounded > 100) {
                rounded = rounded / 10 * 10;
            } else {
                rounded = rounded / 5 * 5;
            }

            return rounded;
        }

        int NextLevelToPush => Level.BaseInt + 1;
        
        // === Initialization
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public HeroDevelopment() { }

        protected override void OnInitialize() {
            AddElement(new WyrdSoulFragments());
            AddElement(new WyrdSkillActivation());
            Init();
        }

        protected override void OnRestore() {
            Init();
        }

        void Init() {
            ParentModel.ListenTo(Stat.Events.StatChanged(HeroStatType.XP), CheckNewLevel, this);
            ParentModel.ListenTo(Stat.Events.ChangingStat(HeroStatType.XP), AnnounceXPChanged, this);
            World.EventSystem.ListenTo(EventSelector.AnySource, StoryFlags.Events.FlagChanged, this, OnFlagChanged);
            World.EventSystem.ListenTo(EventSelector.AnySource, QuestUtils.Events.QuestCompleted, this, OnQuestCompleted);
            World.EventSystem.ListenTo(EventSelector.AnySource, QuestUtils.Events.ObjectiveCompleted, this, OnObjectiveCompleted);
            ParentModel.ListenTo(Stat.Events.StatChangedBy(HeroStatType.WyrdWhispers), AnnounceWhisperChanged, this);
            ParentModel.ListenTo(Stat.Events.StatChangedBy(HeroStatType.WyrdMemoryShards), AnnounceMemoryShardChanged, this);

            UnlockDefaultTalents();
        }

        // === Operations
        public void RewardExpAsPercentOfNextLevel(float expPercent) {
            XP.IncreaseBy(CalculateIncomingExpReward(expPercent));
        }

        public int CalculateIncomingExpReward(float expPercent) {
            float exp = expPercent * RequiredExpFor(Level.ModifiedInt + 1);
            return RoundExp(exp);
        }

        void OnObjectiveCompleted(Objective objective) {
            XP.IncreaseBy(objective.ExperiencePoints);
        }

        void OnQuestCompleted(QuestUtils.QuestStateChange questState) {
            XP.IncreaseBy(questState.quest.ExperiencePoints);
        }

        // === Callbacks
        void CheckNewLevel(Stat stat) {
            if (_levelUpInProgress) {
                return;
            }
            
            while (XP.BaseValue >= RequiredExpFor(NextLevelToPush)) {
                PushNewLevel(NextLevelToPush);
            }
        }

        void AnnounceXPChanged(HookResult<IWithStats, Stat.StatChange> hookResult) {
            AnnounceXPChanged(hookResult.Value.value);
        }

        void AnnounceXPChanged(float gainedXP) {
            if (gainedXP <= 0) {
                return;
            }

            AdvancedNotificationBuffer.Push<ExpNotificationBuffer>(new ExpNotification(gainedXP));
        }
        
        void AnnounceWhisperChanged(Stat.StatChange statChange) {
            if (statChange.value > 0) {
                string info = LocTerms.WyrdWhispers.Translate();
                AnnounceWyrdInfo(info);
            }
        }

        void AnnounceMemoryShardChanged(Stat.StatChange statChange) {
            if (statChange.value > 0) {
                string info = LocTerms.WyrdMemoryShards.Translate();
                AnnounceWyrdInfo(info);
            }
        }
        
        void AnnounceWyrdInfo(string info) {
            AdvancedNotificationBuffer.Push<WyrdInfoNotificationBuffer>(new WyrdInfoNotification(info));
        }

        void OnFlagChanged(string flag) {
            bool isWhisper = false;
            foreach (var whisperFlag in WyrdWhisperFlags) {
                if (string.Equals(whisperFlag, flag, StringComparison.InvariantCulture)) {
                    isWhisper = true;
                    break;
                }
            }
            if (!isWhisper) {
                return;
            }
            
            if (_wyrdWhisperFlagsCollected is { IsCreated: false }) {
                _wyrdWhisperFlagsCollected = new StructList<string>(1);
                _wyrdWhisperFlagsCollected.Add(flag);
                WyrdWhispers.IncreaseBy(1);
                return;
            }

            foreach (var collected in _wyrdWhisperFlagsCollected) {
                if (string.Equals(collected, flag, StringComparison.InvariantCulture)) {
                    return;
                }
            }
            
            _wyrdWhisperFlagsCollected.Add(flag);
            WyrdWhispers.IncreaseBy(1);
        }

        // === Helpers
        void PushNewLevel(int newLevel, bool withNotification = true) {
            _levelUpInProgress = true;
            
            var constants = Services.Get<GameConstants>();
            int maxLevel = constants.maxHeroLevel;
            
            if (Level < maxLevel) {
                Level.IncreaseBy(1);
                if (Level.ModifiedInt % constants.talentEveryNLevel == 0) {
                    TalentPoints.IncreaseBy(1);
                    this.AddMarkerElement(() => new HeroTalentPointsAvailableMarker());
                }

                BaseStatPoints.IncreaseBy(1);
                ParentModel.RefillBaseStats();
                XPForNextLevel.SetTo(RequiredExpFor(NextLevelToPush));

                this.AddMarkerElement(() => new HeroStatPointsAvailableMarker());

                // events
                ParentModel.Trigger(Hero.Events.LevelUp, newLevel);
                if (withNotification) {
                    AdvancedNotificationBuffer.Push<HeroLevelUpNotificationBuffer>(new HeroLevelUpNotification(newLevel));
                }
            }

            XP.DecreaseBy(RequiredExpFor(newLevel));
            _levelUpInProgress = false;
        }

        public int LevelUpTo(int targetLevel) {
            int grantedExp = 0;

            if (Level.BaseInt >= targetLevel) {
                return 0;
            }
            
            while (Level.BaseInt < targetLevel) {
                grantedExp += XPForNextLevel.ModifiedInt - XP.ModifiedInt;
                PushNewLevel(NextLevelToPush, false);
            }
            
            AdvancedNotificationBuffer.Push<HeroLevelUpNotificationBuffer>(new HeroLevelUpNotification(targetLevel));
            
            return grantedExp;
        }

        [UnityEngine.Scripting.Preserve]
        public void RewardFirstTimeLeveling() {
            if (World.Services.Get<SceneService>().IsPrologue) {
                int targetLevel = Level.ModifiedInt + Hero.Template.CampfireTutorialAddLevels;
                LevelUpTo(targetLevel);
                TalentPoints.IncreaseBy(Hero.Template.CampfireTutorialAddTalentPoints);
                BaseStatPoints.IncreaseBy(Hero.Template.CampfireTutorialAddBaseStats);
            }
        }

        public void SetActiveHeavyAttacksUnlock(bool enable) => CanUseHeavyAttack = enable;
        public void SetActiveBowZoomUnlock(bool enable) => CanZoomBow = enable;
        public void SetActiveDashesUnlock(bool enable) => CanDash = enable;
        public void SetActiveSlidesUnlock(bool enable) => CanSlide = enable;
        public void SetActiveSprintAttacksUnlock(bool enable) => CanSprintAttack = enable;
        public void SetActivePommelUnlock(bool enable) => CanPommel = enable;
        public void SetActiveParryUnlock(bool enable) => CanParry = enable;
        public void SetActiveSpectralWeaponsPenetrateShields(bool enable) => SpectralWeaponsPenetrateShields = enable;
        public void SetActiveIncreasedFishingRodsDurability(bool enable) => IncreasedFishingRodsDurability = enable;

        public void SetActiveHeavyArmorNoStaminaUsageIncrease(bool enable) {
            HeavyArmorNoStaminaUsageIncrease = enable;
            ParentModel.HeroItems.Trigger(ICharacterInventory.Events.AfterEquipmentChanged, ParentModel.HeroItems);
        }

        public void SetActiveUnlockArmorReducedManaUsage(bool enable) {
            ArmorReducedManaUsage = enable;
            ParentModel.HeroItems.Trigger(ICharacterInventory.Events.AfterEquipmentChanged, ParentModel.HeroItems);
        }

        public void SetActiveNoDealDamageToSummons(bool enable) => DontDealDamageToSummons = enable;
        public void SetActiveAdditionalPlantsGatheringUnlock(bool enable) => CanGatherAdditionalPlants = enable;
        public void SetActiveDashingWhileAimingUnlock(bool enable) => CanDashWhileAiming = enable;
        public void SetActiveParryDeflectingProjectiles(bool enable) => CanParryDeflectProjectiles = enable;
        public void SetActiveParryDeflectionTargetsEnemies(bool enable) => ParryDeflectionTargetsEnemies = enable;
        public void SetActiveConsumeLessAlcoholInAlchemy(bool enable) => ConsumeLessAlcoholInAlchemy = enable;
        public void SetActiveRemoveNegativeLevelsForCraftingItems(bool enable) => RemoveNegativeLevelsForCraftingItems = enable;
        public void SetActiveCheaperBonfireUpgrade(bool enable) => CheaperBonfireUpgrade = enable;
        public void ChangeActiveBonfireCraftingLevel(bool increase) {
            if (increase) {
                BonfireCraftingLevel++;
            } else {
                BonfireCraftingLevel--;
            }
        }

        void UnlockDefaultTalents() {
            SetActiveHeavyAttacksUnlock(true);
            SetActiveSprintAttacksUnlock(true);
            SetActiveDashesUnlock(true);
            SetActivePommelUnlock(true);
            SetActiveParryUnlock(true);
            SetActiveSlidesUnlock(true);
            SetActiveBowZoomUnlock(true);
            SetActiveNoDealDamageToSummons(true);
        }

        // === Debug
        public void UnlockGameplayTalents() {
            SetActiveHeavyAttacksUnlock(true);
            SetActiveBowZoomUnlock(true);
            SetActiveDashesUnlock(true);
            SetActiveSlidesUnlock(true);
            SetActiveSprintAttacksUnlock(true);
            SetActivePommelUnlock(true);
            SetActiveParryUnlock(true);
            SetActiveHeavyArmorNoStaminaUsageIncrease(true);
            SetActiveUnlockArmorReducedManaUsage(true);
            SetActiveNoDealDamageToSummons(true);
            SetActiveDashingWhileAimingUnlock(true);
            SetActiveParryDeflectingProjectiles(true);
            SetActiveParryDeflectionTargetsEnemies(true);
        }
    }
}