using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Saving.SaveSlots;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    [AttachesTo(typeof(ItemTemplate), AttachmentCategory.ExtraCustom, "Marks item as greed item.")]
    public class GreedItemAttachment : TransformAfterChargesSpentAttachment {
        public override void AfterTransform(Item item) {
            base.AfterTransform(item);
            
            // Remove greed item from all save slots
            string toRemoveGUID = GetComponent<ItemTemplate>().GUID;
            ItemSpawningData itemToRemove = new(new TemplateReference(toRemoveGUID), -1);
            
            // Add the transformed item to all save slots
            string templateGUID = item.Template.GUID;
            ItemSpawningData ensureNoDuplicate = new(new TemplateReference(templateGUID), -1);
            ItemSpawningData itemToAdd = new(new TemplateReference(templateGUID));

            foreach (SaveSlot saveSlot in World.All<SaveSlot>().Where(SaveSlot.SaveSlotBelongsToCurrentHero)) {
                saveSlot.AddItemToModifyOnLoad(itemToRemove, false);
                saveSlot.AddItemToModifyOnLoad(ensureNoDuplicate, false);
                saveSlot.AddItemToModifyOnLoad(itemToAdd, false);
                saveSlot.ApplyChanges();
            }
        }
    }
}
