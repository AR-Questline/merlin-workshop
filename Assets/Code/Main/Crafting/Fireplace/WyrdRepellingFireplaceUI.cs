using System;
using Awaken.TG.Graphics.Transitions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.Heroes.CharacterSheet.Tabs;
using Awaken.TG.Main.Heroes.Resting;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Gems;
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
        const float ForedwellerCrouchDistance = 2.1f;

        const float PetRecallAngleOffset = 100f;
        const float PetRecallDistance = 1.5f;

        readonly TalkData _talkData;
        Location _foredwellerLocation;
        VCManualDissolveController _foredwellerDissolveController;
        Story _createdStory;
        HeroFireplaceInvisibility _invisibility;
        TutorialStage _tutorialStage;

        public bool IsUpgradeable => _talkData.fireplaceLocation?.TryGetElement<LocationStatesElement>();
        public bool HasForedweller => _foredwellerLocation != null;
        public TutorialStage CurrentTutorialStage => _tutorialStage;
        public StoryConfig FordwellerStoryConfig => StoryConfig.Location(_foredwellerLocation, _talkData.foredwellerDialogue, typeof(VDialogue));
        public StoryConfig FordwellerTesterStoryConfig => StoryConfig.Location(_foredwellerLocation, _talkData.foredwellerDialogueTester, typeof(VDialogue));
        float ForedwellerSpawnDistance => ForedwellerCrouchDistance + _talkData.foredwellerSpawnDistance; // Crouch point + distance he travels by animation

        
        public new static class Events {
            public static readonly Event<Hero, WyrdRepellingFireplaceUI> TalkedWithArthurAtCamp = new(nameof(TalkedWithArthurAtCamp));
        }
        
        public WyrdRepellingFireplaceUI(TabSetConfig cookingTabSetConfig, TabSetConfig alchemyTabSetConfig, bool manualRestTime, TalkData talkData,  bool startUpgraded = false) : base(cookingTabSetConfig, alchemyTabSetConfig, manualRestTime, startUpgraded) {
            _talkData = talkData;

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
            _talkData.fireplaceLocation?.TryGetElement<LocationStatesElement>()?.NextState();
        }

        protected override void Resting() {
            ForwardTutorial();
            DestroyForedweller(RestPopupUI.FadeDuration).Forget();
            base.Resting();
        }

        void SpawnForedweller(Vector3 fdPos) {
            if (_talkData.foredwellerTemplate == null) {
                return;
            }
            _foredwellerLocation = _talkData.foredwellerTemplate.SpawnLocation(fdPos, Quaternion.LookRotation((_talkData.fireplaceLocation.Coords - fdPos).X0Z()));
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
            View<VWyrdRepellingFireplaceUI>()?.EnableView(false);
        }

        void EndTalkWithForedweller() {
            _createdStory = null;
            World.Any<HeroLocationInteractionInvolvement>()?.ChangeFocusedLocation();
            UpdateUiVisibility(true);
            if (_talkData.hideAfterNoTalkOptions && Story.IsStorySubMenuEmpty(FordwellerTesterStoryConfig)) {
                DestroyForedweller(null).Forget();
            }
            View<VWyrdRepellingFireplaceUI>()?.RefreshActions();
            View<VWyrdRepellingFireplaceUI>()?.EnableView(true);
        }
        
        public  Model IdentifyLoot() {
            UpdateUiVisibility(false);
            return GemsUI.OpenIdentifyUI();
        }

        public Model FastTravel() {
            UpdateUiVisibility(false);
            return CharacterSheetUI.ToggleCharacterSheet(CharacterSheetTabType.Map, true, CharacterSheetTabType.MapOnlyTabs);
        }
        
        public void RecallPet() {
            RecallPetSequence().Forget();
        }

        async UniTaskVoid RecallPetSequence() {
            var modalBlocker = World.SpawnView(this, typeof(VModalBlocker));
            
            var transition = World.Services.Get<TransitionService>();
            await transition.ToBlack(1f);

            var position = GetPositionAroundFireplace(PetRecallAngleOffset, PetRecallDistance, PetRecallDistance);
            PetUtils.RecallPet(position);

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
            Vector3 dir = new Vector3(_talkData.fireplaceLocation.Coords.x - Hero.Current.Coords.x, 0, _talkData.fireplaceLocation.Coords.z - Hero.Current.Coords.z).normalized;
            dir = Quaternion.AngleAxis(angleOffset, Vector3.up) * dir.normalized;
            var pos = _talkData.fireplaceLocation.Coords + (dir * appearDistance);
            Vector3 groundCheckHeightPos = _talkData.fireplaceLocation.Coords + (dir * groundHeightCheckDistance);
            pos.y = Ground.SnapNpcToGround(groundCheckHeightPos).y;
            return pos;
        }
        
        public enum TutorialStage : byte {
            None,
            TalkWithFD,
            NeedToRest,
        }

        public readonly struct TalkData {
            public readonly LocationTemplate foredwellerTemplate;
            public readonly StoryBookmark foredwellerDialogue;
            public readonly StoryBookmark foredwellerDialogueTester;
            public readonly float foredwellerSpawnDistance;
            public readonly Location fireplaceLocation;
            public readonly bool hideAfterNoTalkOptions;

            public TalkData(LocationTemplate foredwellerTemplate, StoryBookmark foredwellerDialogue, StoryBookmark specForedwellerDialogueTester, float foredwellerSpawnDistance, Location fireplaceLocation, bool hideAfterNoTalkOptions) {
                this.foredwellerTemplate = foredwellerTemplate;
                this.foredwellerDialogue = foredwellerDialogue;
                this.foredwellerDialogueTester = specForedwellerDialogueTester;
                this.foredwellerSpawnDistance = foredwellerSpawnDistance;
                this.fireplaceLocation = fireplaceLocation;
                this.hideAfterNoTalkOptions = hideAfterNoTalkOptions;
            }
        }
    }
}