using System.Diagnostics;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Pickables;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Stories;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Newtonsoft.Json;
using Sirenix.Utilities;

namespace Awaken.TG.Main.Locations.Actions {
    public sealed partial class PickItemAction : AbstractLocationAction, ILocationNameModifier, IRefreshedByAttachment<PickItemAttachment> {
        public override ushort TypeForSerialization => SavedModels.PickItemAction;

        // === Properties
        [Saved] ItemSpawningDataRuntime _itemSpawningData;
        [Saved] bool _destroyedOnInteract;
        bool _triggerDialogueOnInteract;
        StoryBookmark _storyBookmark;

        public int ModificationOrder => 10;
        public override string DefaultActionName =>
            (IsBook, IsIllegal) switch {
                (true, _) => LocTerms.Read.Translate(),
                (_, true) => LocTerms.Steal.Translate(),
                _ => LocTerms.Pickup.Translate()
            };

        protected override InteractRunType RunInteraction => InteractRunType.DontRun;

        public override bool IsIllegal => Crime.Theft(_itemSpawningData, ParentModel).IsCrime();
        bool IsBook => _itemSpawningData?.ItemTemplate?.GetAttachment<ItemReadSpec>() != null;

        public string ModifyName(string original) {
            if (original.IsNullOrWhitespace()) {
                string result = _itemSpawningData?.ItemTemplate?.ItemName;
                if (!result.IsNullOrWhitespace()) {
                    return result;
                }
            }

            return original;
        }
        
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        PickItemAction() {}

        public PickItemAction(ItemSpawningDataRuntime itemSpawningData, bool destroyedOnInteract) {
            _itemSpawningData = itemSpawningData;
            _destroyedOnInteract = destroyedOnInteract;
        }

        public void InitFromAttachment(PickItemAttachment spec, bool isRestored) {
            _itemSpawningData = spec.itemReference.ToRuntimeData(spec);
            _destroyedOnInteract = spec.destroyedAfterInteract;
            _triggerDialogueOnInteract = spec.triggerDialogueOnInteract;
            _storyBookmark = spec.storyBookmark;
        }

        protected override void OnInitialize() {
            base.OnInitialize();
            Validate();
        }

        [Conditional("DEBUG")]
        void Validate() {
            if (_itemSpawningData?.ItemTemplate == null) {
                Log.Important?.Error($"Invalid search location setup in {ParentModel.ID}, spec: {ParentModel.Spec?.gameObject.name}", ParentModel.Spec);
            }
        }

        // === Execution
        protected override void OnStart(Hero hero, IInteractableWithHero interactable) {
            if (_itemSpawningData?.ItemTemplate != null) {
                Item item = new(_itemSpawningData);
                World.Add(item);

                // Readable item case
                if (item.TryGetElement(out ItemRead itemRead)) {
                    item.AddElement(new ItemBeingPicked(ParentModel));
                    itemRead.Submit();
                    Interact(hero, interactable);
                    return;
                }

                CommitCrime.Theft(item, ParentModel);
                hero.Inventory.Add(item);
                ParentModel.Trigger(Events.ItemPicked, new ItemPickedData(ParentModel, item, hero));
            }

            Interact(hero, interactable);
            
            if (_triggerDialogueOnInteract && _storyBookmark != null) {
                Story.StartStory(StoryConfig.Base(_storyBookmark, typeof(VDialogue)));
            }
            
            if (_destroyedOnInteract) {
                interactable.DestroyInteraction();
            }
        }

        public new static class Events {
            public static readonly Event<Location, ItemPickedData> ItemPicked = new(nameof(ItemPicked));
        }

        public struct ItemPickedData {
            [UnityEngine.Scripting.Preserve] public Location location;
            [UnityEngine.Scripting.Preserve] public Item item;
            [UnityEngine.Scripting.Preserve] public ICharacter picker;

            public ItemPickedData(Location location, Item item, ICharacter picker) {
                this.location = location;
                this.item = item;
                this.picker = picker;
            }
        }
    }
}
