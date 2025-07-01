using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.Utility;

namespace Awaken.TG.Main.Locations.Actions {
    public partial class SignPostAction : AbstractLocationAction, ILocationNameModifier, IRefreshedByAttachment<SignPostAttachment> {
        public override ushort TypeForSerialization => SavedModels.SignPostAction;

        public override InfoFrame ActionFrame => new(string.Empty, false);
        public int ModificationOrder => -20;
        protected override InteractRunType RunInteraction => InteractRunType.DontRun;

        string _text;

        public void InitFromAttachment(SignPostAttachment spec, bool isRestored) {
            _text = spec._text.ToString();
        }

        public string ModifyName(string original) {
            return _text;
        }
    }
}