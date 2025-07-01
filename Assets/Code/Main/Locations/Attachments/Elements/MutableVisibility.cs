using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Stories;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class MutableVisibility : Element<Location>, IRefreshedByAttachment<MutableVisibilityAttachment> {
        public override ushort TypeForSerialization => SavedModels.MutableVisibility;

        string _flag;
        bool _ifNoFlag;
        bool _ifFlagTrue;
        bool _ifFlagFalse;
        
        public void InitFromAttachment(MutableVisibilityAttachment spec, bool isRestored) {
            _flag = spec.Flag;
            _ifNoFlag = spec.IfNoFlag;
            _ifFlagTrue = spec.IfFlagTrue;
            _ifFlagFalse = spec.IfFlagFalse;
        }

        protected override void OnInitialize() {
            World.EventSystem.ListenTo(EventSelector.AnySource, StoryFlags.Events.UniqueFlagChanged(_flag), this, Refresh);
            Refresh();
        }

        public void Refresh() {
            ParentModel.SetInteractability(ShouldBeVisible() ? LocationInteractability.Active : LocationInteractability.Hidden);
        }

        bool ShouldBeVisible() {
            var facts = Services.Get<GameplayMemory>().Context();
            if (facts.HasValue(_flag)) {
                return facts.Get<bool>(_flag) ? _ifFlagTrue : _ifFlagFalse;
            } else {
                return _ifNoFlag;
            }
        }
    }
}