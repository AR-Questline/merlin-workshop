using Awaken.TG.Main.Heroes.CharacterCreators;
using Awaken.TG.Main.Settings.Controllers;
using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet {
    [DisallowMultipleComponent]
    public class DisableWhenInCharacterSheet : StartDependentView<GeneralGraphics> {
        
        protected override void OnInitialize() {
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<CharacterSheetUI>(), this, DisableFor);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<CharacterCreator>(), this, DisableFor);
        }
        
        void DisableFor(Model characterSheet) {
            if (gameObject.activeSelf) {
                characterSheet.ListenTo(Model.Events.BeforeDiscarded, EnableFor, this);
                gameObject.SetActive(false);
            }
        }
        
        void EnableFor(Model _) {
            gameObject.SetActive(true);
        }
    }
}
