using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    public partial class OnLogicStateChangedSetInteractability : Element<Location>, IRefreshedByAttachment<OnLogicStateChangedSetInteractabilityAttachment>, ILogicReceiverElement {
        public override ushort TypeForSerialization => SavedModels.OnLogicStateChangedSetInteractability;

        OnLogicStateChangedSetInteractabilityAttachment _spec;
        
        public void InitFromAttachment(OnLogicStateChangedSetInteractabilityAttachment spec, bool isRestored) {
            _spec = spec;
        }

        public void OnLogicReceiverStateChanged(bool state) {
            if (_spec.changeOnEnable && state) {
                ParentModel.SetInteractability(_spec.OnEnableInteractability);
                if (_spec.discardElementAfterEnable) {
                    Discard();
                }
            } else if (_spec.changeOnDisable && !state) {
                ParentModel.SetInteractability(_spec.OnDisableInteractability);
                if (_spec.discardElementAfterDisable) {
                    Discard();
                }
            }
        }
    }
}