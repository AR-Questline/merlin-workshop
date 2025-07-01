using System;
using Awaken.TG.Assets;

namespace Awaken.TG.Main.Locations.Actions {
    public struct UnlockHousingData {
        public string HouseName {get;}
        public string HouseDescription {get;}
        public int Price {get;}
        public ShareableSpriteReference HouseSpriteReference {get;}

        public UnlockHousingData(string houseName, string houseDescription, int price, ShareableSpriteReference houseSpriteReference) {
            HouseName = houseName;
            HouseDescription = houseDescription;
            Price = price;
            HouseSpriteReference = houseSpriteReference;
        }
    }
}