using System;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Development.WyrdPowers;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Gems;
using Awaken.TG.Main.Locations.Pets;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.Stories;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Extensions;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.Fireplace {
    [UsesPrefab("Crafting/VWyrdRepellingFireplaceUI")]
    public class VWyrdRepellingFireplaceUI : VFireplaceUI {
        [Title("Wyrd Fireplace Buttons")]
        [SerializeField] ButtonWithDescription talkWithForedweller; 
        [SerializeField] ButtonWithDescription boostBonfire;
        [SerializeField] ButtonWithDescription restoreWyrdSkill;
        [SerializeField] ButtonWithDescription identifyLoot;
        [SerializeField] ButtonWithDescription fastTravel;
        [SerializeField] ButtonWithDescription alchemy;
        [SerializeField] ButtonWithDescription handcrafting;
        [SerializeField] ButtonWithDescription saveGame;
        [SerializeField] ButtonWithDescription recallPet;
        
        [Title("Cost")]
        [SerializeField] TextMeshProUGUI boostCost;
        [SerializeField] TextMeshProUGUI wyrdSkillRestoreCost;

        int _boostCost;
        int _wyrdSkillRestoreCost;

        public override Component DefaultFocus => CurrentTutorialStage switch {
            WyrdRepellingFireplaceUI.TutorialStage.None => base.DefaultFocus,
            WyrdRepellingFireplaceUI.TutorialStage.TalkWithFD => talkWithForedweller.Button,
            WyrdRepellingFireplaceUI.TutorialStage.NeedToRest => goToSleep.Button,
            _ => throw new ArgumentOutOfRangeException()
        };
        
        WyrdRepellingFireplaceUI.TutorialStage CurrentTutorialStage => ((WyrdRepellingFireplaceUI)Target).CurrentTutorialStage;

        protected override void OnInitialize() {
            var gameConstants = Services.Get<GameConstants>();
            _boostCost = Hero.Current.Development.CheaperBonfireUpgrade ? gameConstants.bonfireUpgradeCostReduced : gameConstants.bonfireUpgradeCost;
            _wyrdSkillRestoreCost = gameConstants.bonfireWyrdSkillRestoreCost;
            
            RefreshActions();

            var ownTarget = (WyrdRepellingFireplaceUI)Target;
            talkWithForedweller.RegisterButton(ownTarget.TalkWithForedweller, LocTerms.FireplaceTalkWithForedweller.Translate(), LocTerms.TalkWithForedwellerDescription.Translate(), ShowDescription);
            boostBonfire.RegisterButton(BoostBonfire, LocTerms.FireplaceBoost.Translate(), LocTerms.BoostDescription.Translate(), ShowDescription);
            restoreWyrdSkill.RegisterButton(RestoreWyrdSkill, LocTerms.FireplaceRestoreWyrdSkill.Translate(), LocTerms.RestoreWyrdSkillDescription.Translate(), ShowDescription);
            identifyLoot.RegisterButton(IdentifyLoot, LocTerms.IdentifyTab.Translate(), LocTerms.IdentifyDescription.Translate(), ShowDescription);
            fastTravel.RegisterButton(ownTarget.FastTravel, LocTerms.FastTravelPopupTitle.Translate(), LocTerms.FastTravelDescription.Translate(), ShowDescription);
            alchemy.RegisterButton(Target.AlchemyAction, LocTerms.Alchemy.Translate(), LocTerms.FireplaceAlchemyDescription.Translate(), ShowDescription);
            handcrafting.RegisterButton(Target.HandcraftingAction, LocTerms.Handcrafting.Translate(), LocTerms.FireplaceHandcraftingDescription.Translate(), ShowDescription);
            saveGame.RegisterButton(Target.SaveGame, LocTerms.SaveGame.Translate(), LocTerms.SaveGameDescription.Translate(), ShowDescription);
            recallPet.RegisterButton(ownTarget.RecallPet, LocTerms.FireplaceRecallPet.Translate(), LocTerms.RecallPetDescription.Translate(), ShowDescription);
            
            boostCost.text = _boostCost.ToString();
            wyrdSkillRestoreCost.text = _wyrdSkillRestoreCost.ToString();
            
            base.OnInitialize();
        }

        static void IdentifyLoot() {
            GemsUI.OpenIdentifyUI();
        }
        
        void BoostBonfire() {
            if (!CanAfford(_boostCost)) {
                return;
            }

            PayForAction(_boostCost);
            Target.Upgrade();
            RefreshActions();
        }

        void RestoreWyrdSkill() {
            if (!CanAfford(_wyrdSkillRestoreCost)) {
                return;
            }
            
            PayForAction(_wyrdSkillRestoreCost);
            World.Only<WyrdSkillActivation>().RestoreSkillDuration();
            RefreshActions();
        }

        static void PayForAction(int costInCobweb) {
            Hero.Current.Cobweb.DecreaseBy(costInCobweb);
        }

        static bool CanAfford(int costInCobweb) {
            return Hero.Current.Cobweb >= costInCobweb;
        }
        
        public void RefreshActions() {
            talkWithForedweller.SetActive(Target is WyrdRepellingFireplaceUI { HasForedweller: true } wyrdRepelling && !Story.IsStorySubMenuEmpty(wyrdRepelling.FordwellerTesterStoryConfig));
            boostBonfire.SetActive(!Target.IsUpgraded && Target is WyrdRepellingFireplaceUI { IsUpgradeable: true });
            identifyLoot.SetActive(Target.IsUpgraded);
            fastTravel.SetActive(Target.IsUpgraded && World.Services.Get<SceneService>().IsOpenWorld);
            alchemy.SetActive(Target.IsUpgraded);
            handcrafting.SetActive(Hero.Current.Development.BonfireCraftingLevel > 0);
            saveGame.SetActive(Target.IsUpgraded && World.Only<DifficultySetting>().Difficulty.SaveRestriction.HasFlagFast(SaveRestriction.SurvivalSaving));
            restoreWyrdSkill.SetActive(World.Only<WyrdSkillActivation>().IsDepleted);
            recallPet.SetActive(World.Any<PetElement>()?.HasBeenLeftBehind() ?? false);

            if (CurrentTutorialStage is WyrdRepellingFireplaceUI.TutorialStage.None) {
                RefreshWyrdSkillRestoreButton();
                if (!Target.IsUpgraded) {
                    RefreshBoostButton();
                }
                ClosePromptActive = true;
                ClosePrompt?.Target?.SetActive(true);
            } else {
                RefreshTutorialActivity();
            }
        }

        void RefreshTutorialActivity() {
            switch (CurrentTutorialStage) {
                case WyrdRepellingFireplaceUI.TutorialStage.None:
                    break;
                case WyrdRepellingFireplaceUI.TutorialStage.TalkWithFD:
                    if (!talkWithForedweller.Active) {
                        ((WyrdRepellingFireplaceUI) Target).ForwardTutorial();
                        RefreshTutorialActivity();
                        return;
                    }
                    talkWithForedweller.Button.Interactable = true;
                    goToSleep.Button.Interactable = false;
                    ClosePromptActive = false;
                    ClosePrompt?.Target?.SetActive(false);
                    SetNonTutorialButtonsInteractability(false);
                    World.Only<Focus>().Select(talkWithForedweller.Button);
                    break;
                case WyrdRepellingFireplaceUI.TutorialStage.NeedToRest:
                    talkWithForedweller.Button.Interactable = true;
                    goToSleep.Button.Interactable = true;
                    ClosePromptActive = false;
                    ClosePrompt?.Target?.SetActive(false);
                    SetNonTutorialButtonsInteractability(true);
                    World.Only<Focus>().Select(goToSleep.Button);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void SetNonTutorialButtonsInteractability(bool interactable) {
            if (interactable) {
                RefreshBoostButton();
                RefreshWyrdSkillRestoreButton();
            } else {
                boostBonfire.Button.Interactable = false;
                restoreWyrdSkill.Button.Interactable = false;
            }

            identifyLoot.Button.Interactable = interactable;
            fastTravel.Button.Interactable = interactable;
            alchemy.Button.Interactable = interactable;
            handcrafting.Button.Interactable = interactable;
            saveGame.Button.Interactable = interactable;

            cooking.Button.Interactable = interactable;
            levelUp.Button.Interactable = interactable;
        } 

        void RefreshBoostButton() {
            bool canAfford = CanAfford(_boostCost);
            boostCost.color = canAfford ? ARColor.MainGrey : ARColor.MainRed;
            boostBonfire.Button.Interactable = canAfford;
        }

        void RefreshWyrdSkillRestoreButton() {
            bool canAfford = CanAfford(_wyrdSkillRestoreCost);
            wyrdSkillRestoreCost.color = canAfford ? ARColor.MainGrey : ARColor.MainRed;
            restoreWyrdSkill.Button.Interactable = canAfford;
        }
    }
}