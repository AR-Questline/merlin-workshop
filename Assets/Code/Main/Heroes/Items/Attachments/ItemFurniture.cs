using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Housing;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Relations;
using Awaken.Utility;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    public partial class ItemFurniture : Element<Item>, IRefreshedByAttachment<ItemFurnitureAttachment> {
        public override ushort TypeForSerialization => SavedModels.ItemFurniture;

        public TemplateReference FurnitureTemplateRef { get; set; }

        public void InitFromAttachment(ItemFurnitureAttachment spec, bool isRestored) {
            FurnitureTemplateRef = spec.furnitureTemplateRef;
        }

        protected override void OnInitialize() {
            ParentModel.ListenTo(IItemOwner.Relations.OwnedBy.Events.BeforeAttached, BeforeAttached, this);
        }
        
        void BeforeAttached(HookResult<IModel, RelationEventData> result) {
            if (result.Value.to is Hero hero) {
                if (FurnitureTemplateRef is { IsSet: true }) {
                    var furnitureVariant = new FurnitureVariant(ParentModel, FurnitureTemplateRef.Get<LocationTemplate>());
                    hero.Element<HeroFurnitures>().LearnFurniture(furnitureVariant);
                }
                
                ParentModel.Discard();
                result.Prevent();
            }
        }
    }
}