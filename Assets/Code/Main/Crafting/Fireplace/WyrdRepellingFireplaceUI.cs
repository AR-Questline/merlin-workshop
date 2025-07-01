using System;
using Awaken.TG.Graphics.Transitions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.Heroes.CharacterSheet.Tabs;
using Awaken.TG.Main.Heroes.Resting;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Pets;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Tutorials;
using Awaken.TG.Main.Utility.VFX;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Universal;
using Awaken.Utility.Maths;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.Fireplace {
    [SpawnsView(typeof(VWyrdRepellingFireplaceUI))]
    public partial class WyrdRepellingFireplaceUI : FireplaceUI {
        const float ForedwellerAngleOffset = 50f;
        const float ForedwellerSpawnDistance = ForedwellerCrouchDistance + 4.6f; // Crouch point + distance he travels by animation
        const float ForedwellerCrouchDistance = 2.1f;

        const float PetRecallAngleOffset = 100f;
        const float PetRecallDistance = 1.5f;

        readonly Location _fireplaceLocation;
        readonly StoryBookmark _foredwellerDialogue;
        readonly StoryBookmark _foredwellerDialogueTester;
        LocationTemplate _foredwellerTemplate;
        Location _foredwellerLocation;
        VCManualDissolveController _foredwellerDissolveController;
        Story _createdStory;
        HeroFireplaceInvisibility _invisibility;
        TutorialStage _tutorialStage;

        public bool IsUpgradeable => _fireplaceLocation?.TryGetElement<LocationStatesElement>();
        public bool HasForedweller => _foredwellerLocation != null;
        public TutorialStage CurrentTutorialStage => _tutorialStage;
        public StoryConfig FordwellerStoryConfig => StoryConfig.Location(_foredwellerLocation, _foredwellerDialogue, typeof(VDialogue));
        public StoryConfig FordwellerTesterStoryConfig => StoryConfig.Location(_foredwellerLocation, _foredwellerDialogueTester, typeof(VDialogue));
        
        public new static class Events {
            public static readonly Event<Hero, WyrdRepellingFireplaceUI> TalkedWithArthurAtCamp = new(nameof(TalkedWithArthurAtCamp));
        }
        
        public WyrdRepellingFireplaceUI(TabSetConfig cookingTabSetConfig, TabSetConfig alchemyTabSetConfig, bool manualRestTime, LocationTemplate foredwellerTemplate, StoryBookmark foredwellerDialogue, StoryBookmark specForedwellerDialogueTester, Location fireplaceLocation = null, bool startUpgraded = false) : base(cookingTabSetConfig, alchemyTabSetConfig, manualRestTime, startUpgraded) {
            _foredwellerTemplate = foredwellerTemplate;
            _foredwellerDialogue = foredwellerDialogue;
            _foredwellerDialogueTester = specForedwellerDialogueTester;
            _fireplaceLocation = fireplaceLocation;

            if (startUpgraded) {
                _invisibility = Hero.Current.AddElement(new HeroFireplaceInvisibility());
            }
        }

        protected override void OnInitialize() {
            if (!Story.IsStorySubMenuEmpty(FordwellerTesterStoryConfig)) {
                var position = GetPositionAroundFireplace(ForedwellerAngleOffset, ForedwellerSpawnDistance, ForedwellerCrouchDistance);
                SpawnForedweller(position);
            }
            _tutorialStage = TutorialMaster.IsBonfireTutorialActive ? TutorialStage.TalkWithFD : TutorialStage.None;
        }

        public void ForwardTutorial() {
            switch (_tutorialStage) {
                case TutorialStage.None:
                    _tutorialStage = TutorialStage.None;
                    break;
                case TutorialStage.TalkWithFD:
                    _tutorialStage = TutorialStage.NeedToRest;
                    break;
                case TutorialStage.NeedToRest:
                    StoryFlags.Set(TutorialMaster.BonfireTutorialFlag, false);
                    _tutorialStage = TutorialStage.None;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override void Upgrade() {
            base.Upgrade();
            _fireplaceLocation?.TryGetElement<LocationStatesElement>()?.NextState();
        }

        protected override void Resting() {
            ForwardTutorial();
            DestroyForedweller(RestPopupUI.FadeDuration).Forget();
            base.Resting();
        }

        void SpawnForedweller(Vector3 fdPos) {
            if (_foredwellerTemplate == null) {
                return;
            }
            _foredwellerLocation = _foredwellerTemplate.SpawnLocation(fdPos, Quaternion.LookRotation((_fireplaceLocation.Coords - fdPos).X0Z()));
            _foredwellerLocation.MarkedNotSaved = true;
            _foredwellerLocation.OnVisualLoaded(t => {
                _foredwellerDissolveController = t.GetComponentInChildren<VCManualDissolveController>();
                _foredwellerDissolveController?.SwitchVisibility(false);
            });
        }

        public void TalkWithForedweller() {
            _createdStory = Story.StartStory(FordwellerStoryConfig);
            
            if (_createdStory is not {HasBeenDiscarded: false}) {
                _createdStory = null;
                return;
            }
            
            ForwardTutorial();
            Hero.Current.Trigger(Events.TalkedWithArthurAtCamp, this);
            _createdStory.ListenTo(Model.Events.AfterDiscarded, EndTalkWithForedweller, this);
            UpdateUiVisibility(false);
        }

        void EndTalkWithForedweller() {
            _createdStory = null;
            World.Any<HeroLocationInteractionInvolvement>()?.ChangeFocusedLocation();
            UpdateUiVisibility(true);
            View<VWyrdRepellingFireplaceUI>()?.RefreshActions();
        }

        public void FastTravel() {
            UpdateUiVisibility(false);
            var characterSheetUI = CharacterSheetUI.ToggleCharacterSheet(CharacterSheetTabType.Map, true, CharacterSheetTabType.MapOnlyTabs);
            characterSheetUI.ListenTo(Model.Events.AfterDiscarded, () => UpdateUiVisibility(true), this);
        }
        
        public void RecallPet() {
            var pet = World.Any<PetElement>();
            if (pet == null) {
                return;
            }
            RecallPetSequence(pet).Forget();
        }

        async UniTaskVoid RecallPetSequence(PetElement pet) {
            var modalBlocker = World.SpawnView(this, typeof(VModalBlocker));
            
            var transition = World.Services.Get<TransitionService>();
            await transition.ToBlack(1f);

            var position = GetPositionAroundFireplace(PetRecallAngleOffset, PetRecallDistance, PetRecallDistance);
            pet.SetFollowing(true);
            pet.TeleportIntoCurrentScene(position);

            if (!await AsyncUtil.DelayTime(this, 0.5f)) {
                return;
            }
            
            View<VWyrdRepellingFireplaceUI>()?.RefreshActions();
            modalBlocker.Discard();
            await transition.ToCamera(1f);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            DestroyForedweller(fromDomainDrop ? 0 : null).Forget();
            if (_invisibility is { HasBeenDiscarded: false }) {
                _invisibility.Discard();
            }
        }

        async UniTaskVoid DestroyForedweller(float? overrideDiscardDelay) {
            if (_foredwellerLocation == null) {
                return;
            }

            if (_foredwellerDissolveController != null && overrideDiscardDelay != 0f) {
                _foredwellerDissolveController.SwitchVisibility(true);
                await AsyncUtil.DelayTime(this, overrideDiscardDelay ?? _foredwellerDissolveController.TotalDissolveTime + 0.2f);
            }

            _foredwellerLocation?.Discard();
            _foredwellerLocation = null;
        }
        
        Vector3 GetPositionAroundFireplace(float angleOffset, float appearDistance, float groundHeightCheckDistance) {
            Vector3 dir = new Vector3(_fireplaceLocation.Coords.x - Hero.Current.Coords.x, 0, _fireplaceLocation.Coords.z - Hero.Current.Coords.z).normalized;
            dir = Quaternion.AngleAxis(angleOffset, Vector3.up) * dir.normalized;
            var pos = _fireplaceLocation.Coords + (dir * appearDistance);
            Vector3 groundCheckHeightPos = _fireplaceLocation.Coords + (dir * groundHeightCheckDistance);
            pos.y = Ground.SnapNpcToGround(groundCheckHeightPos).y;
            return pos;
        }
        
        public enum TutorialStage : byte {
            None,
            TalkWithFD,
            NeedToRest,
        }
    }
}