using Awaken.TG.Main.Heroes.CharacterCreators;
using Awaken.TG.Main.Settings.Controllers;
using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Heroes.CharacterSheet {
    public class DisableWhenInCharacterSheet : StartDependentView<GeneralGraphics> {
        bool _wasActive;
        
        protected override void OnInitialize() {
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<CharacterSheetUI>(), this, DisableFor);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<CharacterCreator>(), this, DisableFor);
        }
        
        void DisableFor(Model characterSheet) {
            characterSheet.ListenTo(Model.Events.BeforeDiscarded, EnableFor, this);
            _wasActive = gameObject.activeSelf;
            gameObject.SetActive(false);
        }
        
        void EnableFor(Model _) {
            gameObject.SetActive(_wasActive);
        }
    }
}
