using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Loadouts;
using Awaken.TG.Main.Heroes.CharacterSheet.QuickUseWheels;
using Awaken.TG.Main.Heroes.CharacterSheet.Tabs;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Items.Loadouts;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.Stories.Quests.Templates;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Main.Tutorials.TutorialPopups;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Main.Utility.UI.Keys;
using Awaken.TG.Main.Utility.UI.Keys.Components;
using Awaken.TG.MVC;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.Tutorials {
    public partial class TutorialMaster {
        public const string BonfireTutorialFlag = "Tutorial:BonfireAwaits";
        public static bool IsBonfireTutorialActive => StoryFlags.Get(BonfireTutorialFlag);
        
        void InitSequences() {
            TutorialSequence.Kill(false, TutorialKeys.AllSequenceKeys);

            if (!World.Only<ShowTutorials>().Enabled || SkipTutorial) {
                return;
            }

            var hero = Hero.Current;
            var config = CommonReferences.Get.TutorialConfig;
            using var tutorialCreationScope = TutorialSequence.Creation;

            TutorialSequence.Create(SequenceKey.OpenInventory)
                 .Append(AfterCustomEvent(hero.HeroItems, ICharacterInventory.Events.PickedUpItem, ItemIsGear, out var pickedUpListener))
                 .Append(Instantly(() => CharacterSheetUI.ChangeLastTab(CharacterSheetTabType.Inventory)))
                 .Append(ShowPrompt(out var promptOpenInventory,
                     LocTerms.TutorialOpenInventory.Translate(),
                     new KeyIcon.Data(KeyBindings.UI.CharacterSheets.Inventory, false).OverrideGamepad(KeyBindings.UI.CharacterSheets.CharacterSheet)
                 ))
                 .Append(AfterCustomEvent(World.Events.ModelAdded<LoadoutsUI>(), out var inventoryAddedListener))
                 .Append(HidePrompt(promptOpenInventory))
                 .OnKill(RemoveListener(pickedUpListener))
                 .OnKill(RemoveListener(inventoryAddedListener))
                 .OnKill(HidePrompt(promptOpenInventory));
            
             TutorialSequence.Create(SequenceKey.LightAttack)
                 .Append(AfterCustomEvent(hero.HeroItems, ICharacterInventory.Events.AnySlotChanged, SlotWithMeleeWeapon, out var lightAttackSlotChangedListener))
                 .Append(ShowPrompt(out var promptLightAttack,
                     LocTerms.TutorialLightAttack.Translate(),
                     new KeyIcon.Data(KeyBindings.Gameplay.Attack, false).OverrideMouse(ControllerKey.Mouse.LeftMouseButton)
                 ))
                 .Append(AfterCustomEvent(hero, ICharacter.Events.OnAttackRelease, arAnimationEvent => arAnimationEvent.attackType is not AttackType.Heavy, out var lightAttackListener))
                 .Append(HidePrompt(promptLightAttack))
                 .OnKill(RemoveListener(lightAttackSlotChangedListener))
                 .OnKill(RemoveListener(lightAttackListener))
                 .OnKill(HidePrompt(promptLightAttack));
            
             TutorialSequence.Create(SequenceKey.HeavyAttack)
                 .Append(AfterCustomEvent(hero.HeroItems, ICharacterInventory.Events.AnySlotChanged, SlotWithMeleeWeapon, out var heavyAttackSlotChangedListener))
                 .Append(ShowPrompt(out var promptHeavyAttack,
                     LocTerms.TutorialHeavyAttack.Translate(),
                     new KeyIcon.Data(KeyBindings.Gameplay.AttackHeavy, true).OverrideMouse(ControllerKey.Mouse.LeftMouseButton)
                 ))
                 .Append(AfterCustomEvent(hero, ICharacter.Events.OnAttackRelease, arAnimationEvent => arAnimationEvent.attackType is AttackType.Heavy, out var heavyAttackListener))
                 .Append(HidePrompt(promptHeavyAttack))
                 .OnKill(RemoveListener(heavyAttackSlotChangedListener))
                 .OnKill(RemoveListener(heavyAttackListener))
                 .OnKill(HidePrompt(promptHeavyAttack));
            
             TutorialSequence.Create(SequenceKey.Block)
                 .Append(AfterCustomEvent(hero.HeroItems, ICharacterInventory.Events.AnySlotChanged, SlotWithMeleeWeapon, out var blockSlotChangedListener))
                 .Append(ShowPrompt(out var promptBlock,
                     LocTerms.TutorialBlock.Translate(),
                     new KeyIcon.Data(KeyBindings.Gameplay.Block, true).OverrideMouse(ControllerKey.Mouse.RightMouseButton)
                 ))
                 .Append(AfterCustomEvent(World.Events.ModelDiscarded<HeroBlock>(), out var blockDiscardedListener))
                 .Append(HidePrompt(promptBlock))
                 .OnKill(RemoveListener(blockSlotChangedListener))
                 .OnKill(RemoveListener(blockDiscardedListener))
                 .OnKill(HidePrompt(promptBlock));
            
             TutorialSequence.Create(SequenceKey.HideWeapon)
                 .Append(AfterCustomEvent(hero.HeroItems, ICharacterInventory.Events.AnySlotChanged, SlotWithMeleeWeapon, out var hideWeaponSlotChangedListener))
                 .Append(ShowPrompt(out var promptHideWeapon,
                     LocTerms.TutorialHideWeapon.Translate(),
                     new KeyIcon.Data(KeyBindings.Gameplay.ToggleWeapon, false)
                 ))
                 .Append(AfterCustomEvent(hero, Hero.Events.HideWeapons, out var hideWeaponListener))
                 .Append(HidePrompt(promptHideWeapon))
                 .OnKill(RemoveListener(hideWeaponSlotChangedListener))
                 .OnKill(RemoveListener(hideWeaponListener))
                 .OnKill(HidePrompt(promptHideWeapon));
            
             TutorialSequence.Create(SequenceKey.ShowActiveObjective)
                 .Append(AfterCustomEvent(QuestUtils.Events.QuestAdded, TrackableQuest, out var showActiveObjectiveChangedListener))
                 .Append(ShowPrompt(out var promptShowActiveObjective,
                     LocTerms.TutorialShowActiveObjective.Translate(),
                     new KeyIcon.Data(KeyBindings.UI.HUD.ToggleQuestTracker, false)
                 ))
                 .Append(AfterCustomEvent(QuestTracker.Events.QuestTrackerClicked, out var questRefreshedListener))
                 .Append(HidePrompt(promptShowActiveObjective))
                 .OnKill(RemoveListener(showActiveObjectiveChangedListener))
                 .OnKill(RemoveListener(questRefreshedListener))
                 .OnKill(HidePrompt(promptShowActiveObjective));
            
             TutorialSequence.Create(SequenceKey.Dash)
                 .Append(AfterCustomEvent(hero, ICharacter.Events.CombatEntered, out var dashCombatEnteredListener))
                 .Append(ShowPrompt(out var promptDash,
                     LocTerms.TutorialDash.Translate(),
                     new KeyIcon.Data(KeyBindings.Gameplay.Dash, false)
                 ))
                 .Append(AfterCustomEvent(hero, Hero.Events.AfterHeroDashed, out var dashListener))
                 .Append(HidePrompt(promptDash))
                 .OnKill(RemoveListener(dashCombatEnteredListener))
                 .OnKill(RemoveListener(dashListener))
                 .OnKill(HidePrompt(promptDash));
            
             TutorialSequence.Create(SequenceKey.Pommel)
                 .Append(AfterCustomEvent(hero, ICharacter.Events.CombatEntered, out var pommelCombatEnteredListener))
                 .Append(ShowPrompt(out var promptPommel,
                     LocTerms.TutorialPush.Translate(),
                     new KeyIcon.Data(KeyBindings.Gameplay.Block, true).OverrideMouse(ControllerKey.Mouse.RightMouseButton),
                     new KeyIcon.Data(KeyBindings.Gameplay.Attack, false).OverrideMouse(ControllerKey.Mouse.LeftMouseButton)
                 ))
                 .Append(AfterCustomEvent(hero, Hero.Events.AfterHeroPommel, out var pommelListener))
                 .Append(HidePrompt(promptPommel))
                 .OnKill(RemoveListener(pommelCombatEnteredListener))
                 .OnKill(RemoveListener(pommelListener))
                 .OnKill(HidePrompt(promptPommel));

             TutorialSequence.Create(SequenceKey.ParryOnCombat) // Run tutorial about Parrying on combat enter
                 .Append(AfterCustomEvent(hero, ICharacter.Events.CombatEntered, IsEnemyParryable, out var parryCombatEnteredListener))
                 .Append(Instantly(() => Trigger(TutKeys.TriggerParry)))
                 .OnKill(RemoveListener(parryCombatEnteredListener));

             TutorialSequence.Create(SequenceKey.ParryOnTrigger) // Run tutorial about Parrying on hero enters trigger
                 .WaitForTrigger(TutKeys.TriggerParry, this, out var parryTrigger)
                 .Append(Instantly(() => TutorialVideo.Show(config.parry)))
                 .Append(Instantly(() => TutorialVideo.Show(config.enemyStamina)))
                 .OnKill(Instantly(()=> TutorialSequence.Kill(true, SequenceKey.ParryOnCombat)))
                 .OnKill(RemoveListener(parryTrigger));
             
             TutorialSequence.Create(SequenceKey.QuickUseWheel)
                 .Append(AfterCustomEvent(hero.HeroItems, ICharacterInventory.Events.AfterEquipmentChanged, HeroHasMoreLoadouts,
                     out var quickUseWheelEquipmentChangedListener))
                 .Append(ShowPrompt(out var promptQuickUseWheel,
                     LocTerms.TutorialQuickUseWheel.Translate(),
                     new KeyIcon.Data(KeyBindings.UI.HUD.QuickUseWheel, false)
                 ))
                 .Append(AfterCustomEvent(World.Events.ModelAdded<QuickUseWheelUI>(), out var quickUseWheelAddedListener))
                 .Append(HidePrompt(promptQuickUseWheel))
                 .OnKill(RemoveListener(quickUseWheelEquipmentChangedListener))
                 .OnKill(RemoveListener(quickUseWheelAddedListener))
                 .OnKill(HidePrompt(promptQuickUseWheel));
            
             TutorialSequence.Create(SequenceKey.SoulFragment1)
                 .WaitForTrigger(TutKeys.TriggerSoulFragment1, this, out var soulFragment1Trigger)
                 .Append(Instantly(() => TutorialVideo.Show(config.soulFragment1)))
                 .OnKill(RemoveListener(soulFragment1Trigger));

             TutorialSequence.Create(SequenceKey.WyrdPower)
                 .WaitForTrigger(TutKeys.TriggerWyrdPower, this, out var wyrdPowerTrigger)
                 .Append(Instantly(() => TutorialVideo.Show(config.wyrdPower)))
                 .Append(Instantly(() => Hero.Current?.HeroStats.WyrdSkillDuration.SetToFull()))
                 .Append(ShowPrompt(out var promptWyrdPower,
                     LocTerms.TutorialWyrdPower.Translate(),
                     new KeyIcon.Data(KeyBindings.Gameplay.UseWyrdSkillsSlot, false)
                 ))
                 .Append(AfterCustomEvent(hero, Hero.Events.WyrdskillToggled, state => !state, out var wyrdPowerListener))
                 .Append(HidePrompt(promptWyrdPower))
                 .OnKill(RemoveListener(wyrdPowerTrigger))
                 .OnKill(RemoveListener(wyrdPowerListener))
                 .OnKill(HidePrompt(promptWyrdPower));
            
             TutorialSequence.Create(SequenceKey.Crouch)
                 .WaitForTrigger(TutKeys.TriggerCrouch, this, out var crouchTrigger)
                 .Append(ShowPrompt(out var promptCrouch,
                     LocTerms.TutorialCrouch.Translate(),
                     new KeyIcon.Data(KeyBindings.Gameplay.Crouch, false)
                 ))
                 .Append(AfterCustomEvent(hero, Hero.Events.HeroCrouchToggled, out var crouchListener))
                 .Append(HidePrompt(promptCrouch))
                 .OnKill(RemoveListener(crouchTrigger))
                 .OnKill(RemoveListener(crouchListener))
                 .OnKill(HidePrompt(promptCrouch));
            
             TutorialSequence.Create(SequenceKey.Sprint)
                 .WaitForTrigger(TutKeys.TriggerSprint, this, out var sprintTrigger)
                 .Append(Instantly(() => Trigger(TutKeys.TriggerWalk)))
                 .Append(ShowPrompt(out var promptSprint,
                     LocTerms.TutorialSprint.Translate(),
                     new KeyIcon.Data(KeyBindings.Gameplay.Sprint, false)
                 ))
                 .Append(AfterCustomEvent(hero, Hero.Events.HeroSprintingStateChanged, out var sprintListener))
                 .Append(HidePrompt(promptSprint))
                 .OnKill(RemoveListener(sprintTrigger))
                 .OnKill(RemoveListener(sprintListener))
                 .OnKill(HidePrompt(promptSprint));
            
             TutorialSequence.Create(SequenceKey.Walk)
                 .PersistentCondition(() => !RewiredHelper.IsGamepad)
                 .WaitForTrigger(TutKeys.TriggerWalk, this, out var walkTrigger)
                 .Append(ShowPrompt(out var promptWalk,
                     LocTerms.TutorialWalk.Translate(),
                     new KeyIcon.Data(KeyBindings.Gameplay.Walk, false)
                 ))
                 .Append(AfterCustomEvent(hero, Hero.Events.HeroWalkingStateChanged, out var walkStateListener))
                 .Append(HidePrompt(promptWalk))
                 .OnKill(RemoveListener(walkTrigger))
                 .OnKill(RemoveListener(walkStateListener))
                 .OnKill(HidePrompt(promptWalk));
            
             TutorialSequence.Create(SequenceKey.SetUpCamp)
                 .WaitForTrigger(TutKeys.TriggerSetUpCamp, this, out var openEqTrigger)
                 .Append(Instantly(() => {
                     TutorialVideo.Show(config.bonfire);
                     StoryFlags.Set(BonfireTutorialFlag, true);
                 }))
                 .OnKill(RemoveListener(openEqTrigger));
             
            TutorialSequence.Create(SequenceKey.SpyglassAcquire)
                .Append(AfterCustomEvent(hero.HeroItems, ICharacterInventory.Events.PickedUpItem, item => item.TryGetElement(out Tool element) && element.Type == ToolType.Spyglassing, out var spyglassAcquireListener))
                .Append(Instantly(() => TutorialVideo.Show(config.spyglass)))
                .OnKill(RemoveListener(spyglassAcquireListener));
            
            // first rod tutorial - this is tutorial when player gets fishing rod for the first time
            TutorialSequence.Create(SequenceKey.FishingRodAcquire)
                .Append(AfterCustomEvent(hero.HeroItems, ICharacterInventory.Events.PickedUpItem, item => item.TryGetElement(out Tool element) && element.Type == ToolType.Fishing, out var fishingRodAcquireListener))
                .Append(Instantly(() => TutorialVideo.Show(config.fishingRod)))
                .OnKill(RemoveListener(fishingRodAcquireListener));
            
            TutorialSequence.Create(SequenceKey.SketchbookAcquire)
                .Append(AfterCustomEvent(hero.HeroItems, ICharacterInventory.Events.PickedUpItem, item => item.TryGetElement(out Tool element) && element.Type == ToolType.Sketching, out var sketchbookAcquireListener))
                .Append(Instantly(() => TutorialVideo.Show(config.sketchbook)))
                .OnKill(RemoveListener(sketchbookAcquireListener));
            
            TutorialSequence.Create(SequenceKey.HorseAcquire)
                .WaitForTrigger(TutKeys.TriggerHorseAcquire, this, out var horseAcquireTrigger)
                .Append(Instantly(() => TutorialGraphic.Show(config.Horse)))
                .OnKill(RemoveListener(horseAcquireTrigger));
            
            TutorialSequence.Create(SequenceKey.HorseArmorDlc)
                .WaitForTrigger(TutKeys.TriggerHorseArmorDlc, this, out var horseArmorDlcTrigger)
                .Append(Instantly(() => TutorialGraphic.Show(config.horseArmorDlc)))
                .OnKill(RemoveListener(horseArmorDlcTrigger));
            
            TutorialSequence.Create(SequenceKey.RedDeathSkillTreeUnlock)
                .WaitForTrigger(TutKeys.TriggerRedDeathSkillTreeUnlock, this, out var redDeathSkillTreeUnlockTrigger)
                .Append(Instantly(() => TutorialGraphic.Show(config.redDeathSkillTree)))
                .OnKill(RemoveListener(redDeathSkillTreeUnlockTrigger));
            
            TutorialSequence.Create(SequenceKey.FirstMemoryShardAcquire)
                .WaitForTrigger(TutKeys.TriggerFirstMemoryShardAcquire, this, out var firstShardAcquireTrigger)
                .Append(Instantly(() => TutorialGraphic.Show(config.firstMemoryShardAcquire)))
                .OnKill(RemoveListener(firstShardAcquireTrigger));
            
            TutorialSequence.Create(SequenceKey.FirstWyrdWhisperAcquire)
                .WaitForTrigger(TutKeys.TriggerFirstWyrdWhisperAcquire, this, out var firstWyrdWhisperAcquireTrigger)
                .Append(Instantly(() => TutorialGraphic.Show(config.firstWyrdWhisperAcquire)))
                .OnKill(RemoveListener(firstWyrdWhisperAcquireTrigger));
             
            // seconds fishing tutorial - this is tutorial for the fishing mini game - after players started using it
            TutorialSequence.Create(SequenceKey.Fishing)
                .Append(AfterCustomEvent(hero, FishingFSM.Events.BobberHitWater, out var fishingThrowListener))
                .Append(Instantly(() => TutorialVideo.Show(config.fishing)))
                .OnKill(RemoveListener(fishingThrowListener));
        }

        static bool TrackableQuest(QuestUtils.QuestStateChange stateChange) {
            return stateChange.quest.Template is not AchievementTemplate && stateChange.newState == QuestState.Active;
        }
        
        static bool ItemIsGear(Item item) {
            return item is { IsGear: true };
        }
        
        static bool IsEnemyParryable(ICharacter character) {
            var attackers = character.PossibleAttackers.GetEnumerator();
            attackers.MoveNext();
            if (attackers.Current == null) {
                attackers.Dispose();
                return false;
            }
            
            return TagUtils.HasRequiredTag(attackers.Current, CommonReferences.Get.TutorialConfig.parryableEnemyTag);
        }

        static bool SlotWithMeleeWeapon(EquipmentSlotType slot) {
            return Hero.Current.HeroItems.EquippedItem(slot) is { IsMelee: true, IsWeapon: true, IsFists: false };
        }

        static bool HeroHasMoreLoadouts(ICharacterInventory inventory) {
            int count = 0;
            foreach (var loadout in inventory.Elements<HeroLoadout>()) {
                if (loadout.PrimaryItem?.IsFists == false || loadout.SecondaryItem?.IsFists == false) {
                    count++;
                }
            }

            return count > 1;
        }
    }
}