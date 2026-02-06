using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.CharacterSheet.Character;
using Awaken.TG.Main.Heroes.CharacterSheet.Tabs;
using Awaken.TG.Main.Heroes.HUD;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Locations.Actions {
    public partial class SarrasShrineAction : AbstractLocationAction, IRefreshedByAttachment<SarrasShrineAttachment> {
        public override ushort TypeForSerialization => SavedModels.SarrasShrineAction;
        [Saved] bool _prayed;
        
        protected override InfoFrame ActionFrameInternal => new(LocTerms.FireplacePray.Translate(), true);
        public bool PointsDistributionInProgress { get; private set; }
        
        StoryBookmark _bookmark;
        TemplateReference _statusReference;

        public void InitFromAttachment(SarrasShrineAttachment spec, bool isRestored) {
            if (spec.bookmark == null || !spec.bookmark.IsValid) {
                LogInvalidSetup(spec, "StoryBookmark");
            }
            if (spec.statusReference == null || !spec.statusReference.IsSet) {
                LogInvalidSetup(spec, "StatusReference");
            }
            
            _bookmark = spec.bookmark;
            _statusReference = spec.statusReference;
        }

        protected override void OnInitialize() {
            if (_prayed) {
                ParentModel.SetInteractability(LocationInteractability.Active);
            }
        }

        protected override void OnStart(Hero hero, IInteractableWithHero interactable) {
            if (_prayed) {
                OpenSarrasTreeUI().Forget();
            } else {
                _prayed = true;
                Story.StartStory(StoryConfig.Interactable(interactable, _bookmark, null));
                GiveStatus();
                ShowBlessingUI();
            }
        }
        
        void GiveStatus() {
            var statusTemplate = _statusReference.Get<StatusTemplate>();
            var sourceInfo = StatusSourceInfo.FromStatus(statusTemplate);
            var hero = Hero.Current;
            sourceInfo.WithCharacter(hero);
            hero.Statuses.AddStatus(statusTemplate, sourceInfo);
        }

        void ShowBlessingUI() {
            ParentModel.SetInteractability(LocationInteractability.Inactive);
            
            var shrineUI = World.Only<HUD>().AddElement(new SarrasShrineBlessingUI());
            shrineUI.ListenTo(Events.BeforeDiscarded, () => {
                ParentModel.SetInteractability(LocationInteractability.Active);
                World.Any<HeroInteractionUI>()?.TriggerChange();
            }, this);
        }

        async UniTaskVoid OpenSarrasTreeUI() {
            PointsDistributionInProgress = true;
            CharacterUI characterUI = await CharacterUI.ToggleCharacterSheet(CharacterSubTabType.SarrasTalents, true, CharacterSheetTabType.LevelUpTabs, true);
            
            characterUI.ListenTo(Events.BeforeDiscarded, () => PointsDistributionInProgress = false);
        }

        void LogInvalidSetup(SarrasShrineAttachment spec, string element) {
            Log.Minor?.Error($"SarrasShrineAttachment without {element} assigned! " + LogUtils.GetDebugName(this) + " - " + spec.gameObject.name, spec.gameObject);
        }
    }
}