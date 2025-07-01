using System;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.EntryInfo {
    public partial class EntryInfoUI : Element<IElement> {
        public sealed override bool IsNotSaved => true;

        public readonly string entryDescription;

        readonly Type _entryInfoViewType;
        
        public EntryInfoUI(string description, Type viewType) {
            entryDescription = description;
            _entryInfoViewType = viewType;
        }

        protected override void OnFullyInitialized() {
            World.SpawnView(this, _entryInfoViewType, true);
        }
    }
}