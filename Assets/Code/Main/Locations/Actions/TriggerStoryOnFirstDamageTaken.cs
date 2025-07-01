using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Stories;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;

namespace Awaken.TG.Main.Locations.Actions {
    public partial class TriggerStoryOnFirstDamageTaken : Element<Location>, IRefreshedByAttachment<TriggerStoryOnFirstDamageTakenAttachment> {
        public override ushort TypeForSerialization => SavedModels.TriggerStoryOnFirstDamageTaken;

        StoryBookmark _bookmark;
        
        public void InitFromAttachment(TriggerStoryOnFirstDamageTakenAttachment spec, bool isRestored) {
            _bookmark = spec.bookmark;
        }

        protected override void OnInitialize() {
            ParentModel.AfterFullyInitialized(() => {
                ParentModel.Character?.HealthElement.ListenToLimited(HealthElement.Events.OnDamageTaken, OnDamageTaken, this);
            }, this);
        }

        void OnDamageTaken(DamageOutcome damageOutcome) {
            if (damageOutcome.Damage.DamageDealer is Hero && (ParentModel.Character?.IsAlive ?? false)) {
                Story.StartStory(StoryConfig.Location(ParentModel, _bookmark, null));
            }
            Discard();
        }
    }
}