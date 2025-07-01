using System.Collections.Generic;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Tutorials.TutorialPrompts;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI.Keys;
using Awaken.TG.Main.Utility.UI.Keys.Components;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Tutorials {
    public partial class ToolsTutorials : Element<Hero> {
        public sealed override bool IsNotSaved => true;

        HeroItems HeroItems => ParentModel.HeroItems;

        List<TutorialPrompt> _prompts = new();

        protected override void OnInitialize() {
            ParentModel.AfterFullyInitialized(AfterHeroFullyInitialized);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            CleanUpPrompts();
        }

        void AfterHeroFullyInitialized() {
            HeroItems.ListenTo(ICharacterInventory.Events.AnySlotChanged, OnSlotChanged, this);
        }

        void OnSlotChanged(EquipmentSlotType slot) {
            // Only care about the first slot, as that's where the tools are equipped
            if (slot is not { Index: 0 }) {
                return;
            }
            
            CleanUpPrompts();
            var equippedItem = HeroItems.EquippedItem(slot);
            if (equippedItem != null && equippedItem.TryGetElement(out Tool element)) {
                if (element.Type == ToolType.Spyglassing) {
                    ShowPrompt(LocTerms.TutorialSpyglassUse.Translate(), new KeyIcon.Data(KeyBindings.Gameplay.Attack, false).OverrideMouse(ControllerKey.Mouse.LeftMouseButton), null);
                    ShowPrompt(LocTerms.TutorialSpyglassSetMarker.Translate(), new KeyIcon.Data(KeyBindings.Gameplay.Interact, false), null);
                } else if (element.Type == ToolType.Fishing) {
                    //ShowPrompt(LocTerms.TutorialFishingRodUse.Translate(), new KeyIcon.Data(KeyBindings.Gameplay.Interact, false), null); // not needed for now
                } else if (element.Type == ToolType.Sketching) {
                    ShowPrompt(LocTerms.TutorialSketchbookUse.Translate(), new KeyIcon.Data(KeyBindings.Gameplay.Interact, false), null);
                } else if (element.Type == ToolType.Mining) {
                    //ShowPrompt(LocTerms.TutorialPickaxeUse.Translate(), new KeyIcon.Data(KeyBindings.Gameplay.Interact, false), null); // not needed for now
                } else if (element.Type == ToolType.Digging) {
                    //ShowPrompt(LocTerms.TutorialShovelUse.Translate(), new KeyIcon.Data(KeyBindings.Gameplay.Interact, false), null); // not needed for now
                } else {
                    Log.Debug?.Warning($"Tutorials for {element.Type} not supported.");
                }
            }
        }

        void ShowPrompt(string description, KeyIcon.Data key0, KeyIcon.Data? key1) {
            _prompts.Add(TutorialPrompt.Show(description, key0, key1));
        }

        void CleanUpPrompts() {
            for (int i = _prompts.Count - 1; i >= 0; i--) {
                if (!_prompts[i].HasBeenDiscarded) {
                    _prompts[i].Discard();
                }
            }

            _prompts.Clear();
        }
    }
}