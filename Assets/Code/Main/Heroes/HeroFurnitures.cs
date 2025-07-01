using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Housing;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;

namespace Awaken.TG.Main.Heroes {
    public partial class HeroFurnitures : Element<Hero> {
        public override ushort TypeForSerialization => SavedModels.HeroFurnitures;
        
        [Saved] HashSet<FurnitureVariant> _furnitureVariants = new();
        
        public HashSet<FurnitureVariant> FurnitureVariants => _furnitureVariants;
        
        public void LearnFurniture(FurnitureVariant furniture) {
            _furnitureVariants.Add(furniture);
        }
    }
}