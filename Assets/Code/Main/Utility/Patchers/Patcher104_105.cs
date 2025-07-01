using System;
using System.Linq;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Housing;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Templates;

namespace Awaken.TG.Main.Utility.Patchers {
    public class Patcher104_105 : Patcher {
        protected override Version MaxInputVersion => new(1, 4, 9999);
        protected override Version FinalVersion => new(1, 5, 0);

        public override void AfterRestorePatch() {
            var hero = Hero.Current;
            if (hero == null) {
                // we are probably on the title screen
                return;
            }
            
            var heroFurniture = hero.TryGetElement<HeroFurnitures>() ?? hero.AddElement(new HeroFurnitures());
            
            const string WoodenWillaGuid = "63ff856faa5336e48a6ac2cefe9b8efb";
            const string ResidenceGuid = "2e65300fb8c00ce4484c99d1a08a2db9";
            const string InitialFurnitures = "a8ff8471e980b5c459460d57de39db16";
            
            var heroItems = hero.HeroItems.Items.ToList();
            bool anyHouseUnlocked = heroItems.Any(item => item.Template.GUID is WoodenWillaGuid or ResidenceGuid);

            if (anyHouseUnlocked && StoryBookmark.ToInitialChapter(new TemplateReference(InitialFurnitures), out var bookmark)) {
                Story.StartStory(StoryConfig.Base(bookmark, null));
                foreach (Item heroItem in heroItems) {
                    var furniture = heroItem.TryGetElement<ItemFurniture>();
                    if (furniture != null) {
                        var furnitureVariant = new FurnitureVariant(heroItem, furniture.FurnitureTemplateRef.Get<LocationTemplate>());
                        heroFurniture.LearnFurniture(furnitureVariant);
                        heroItem.Discard();
                    }
                }
            }
        }
    }
}