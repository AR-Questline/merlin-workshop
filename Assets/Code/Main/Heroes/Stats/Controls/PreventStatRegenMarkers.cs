using Awaken.TG.Main.Character;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Heroes.Stats.Controls {
    public interface IPreventStaminaRegen : IElement<ICharacter> {
        // === Events
        public static class Events {
            public static readonly Event<ICharacter, bool> StaminaRegenBlocked = new(nameof(StaminaRegenBlocked));
        }
    }
}