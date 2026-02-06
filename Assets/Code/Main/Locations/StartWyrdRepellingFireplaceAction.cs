using Awaken.TG.Main.Crafting.Fireplace;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.SocialServices;
using Awaken.TG.Main.Stories;
using Awaken.TG.MVC;
using Awaken.Utility;

namespace Awaken.TG.Main.Locations {
    public partial class StartWyrdRepellingFireplaceAction : StartFireplaceBaseAction, IRefreshedByAttachment<StartWyrdrepellingFireplaceAttachment> {
        public override ushort TypeForSerialization => SavedModels.StartWyrdRepellingFireplaceAction;

        StartWyrdrepellingFireplaceAttachment _spec;
        
        protected override bool ManualRestTime => _spec.ManualRestTime;

        public void InitFromAttachment(StartWyrdrepellingFireplaceAttachment spec, bool isRestored) {
            _cookingTabSetConfig = spec.TabSetSetConfig;
            _alchemyTabSetConfig = spec.AlchemyTabSetSetConfig;
            _spec = spec;
        }

        protected override void InitUI() {
            var fireplace = World.Any<WyrdRepellingFireplaceUI>();
            if (fireplace == null) {
                TryGetTalkData(out var talkingLocationTemplate, out var dialogue, out var dialogueTester, out var spawnDistance, out var hideAfterNoTalkOptions);
                var talkData = new WyrdRepellingFireplaceUI.TalkData(talkingLocationTemplate, dialogue, dialogueTester, spawnDistance, ParentModel, hideAfterNoTalkOptions);
                fireplace = World.Add(new WyrdRepellingFireplaceUI(_cookingTabSetConfig, _alchemyTabSetConfig, _spec.ManualRestTime, talkData, _spec.IsUpgraded));
            } else {
                fireplace.View<VWyrdRepellingFireplaceUI>().RefreshActions();
            }
            
            fireplace.ListenToLimited(Events.AfterDiscarded, () => EndFireplaceInteraction(false), this);
        }

        void TryGetTalkData(out LocationTemplate talkingLocationTemplate, out StoryBookmark dialogue, out StoryBookmark dialogueTester, out float spawnDistance, out bool hideAfterNoTalkOptions) {
            var facts = World.Services.Get<GameplayMemory>().Context();
            for (int i = 0; i < _spec.TalkConfigs.Length; i++) {
                if (!string.IsNullOrWhiteSpace(_spec.TalkConfigs[i].disablingFlag) && facts.Get<bool>(_spec.TalkConfigs[i].disablingFlag)) {
                    continue;
                }
                if (_spec.TalkConfigs[i].requireDLC && !SocialService.Get.HasDlc(_spec.TalkConfigs[i].requiredDLC)) {
                    continue;
                }
                bool valid = true;
                for (int j = 0; j < _spec.TalkConfigs[i].requiredFlags.Length; j++) {
                    if (!string.IsNullOrWhiteSpace(_spec.TalkConfigs[i].requiredFlags[j]) && !facts.Get<bool>(_spec.TalkConfigs[i].requiredFlags[j])) {
                        valid = false;
                        break;
                    }
                }
                if (valid) {
                    talkingLocationTemplate = _spec.TalkConfigs[i].talkingLocation.Get<LocationTemplate>(this);
                    dialogue = _spec.TalkConfigs[i].dialogue;
                    dialogueTester = _spec.TalkConfigs[i].dialogueTester;
                    spawnDistance = _spec.TalkConfigs[i].spawnDistance;
                    hideAfterNoTalkOptions = _spec.TalkConfigs[i].hideAfterNoTalkOptions;
                    return;
                }
            }

            talkingLocationTemplate = null;
            dialogue = null;
            dialogueTester = null;
            spawnDistance = 0;
            hideAfterNoTalkOptions = false;
        }
    }
}