using Awaken.TG.Main.Character;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Heroes.Stats.Controls {
    /// <summary>
    /// Marker class for manually managing stamina regen preventing without duration
    /// </summary>
    public partial class PreventStaminaRegenMarker : Element<ICharacter>, IPreventStaminaRegen {
        public sealed override bool IsNotSaved => true;

        protected override void OnFullyInitialized() {
            ParentModel.Trigger(IPreventStaminaRegen.Events.StaminaRegenBlocked, true);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            ParentModel.Trigger(IPreventStaminaRegen.Events.StaminaRegenBlocked, false);
        }
    }
}