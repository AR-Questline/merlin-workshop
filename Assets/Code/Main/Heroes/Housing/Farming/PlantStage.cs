using System;
using Awaken.TG.Assets;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Times;

namespace Awaken.TG.Main.Heroes.Housing.Farming {
    [Serializable]
    public partial struct PlantStage {
        public ushort TypeForSerialization => SavedTypes.PlantStage;

        [Saved, PrefabAssetReference(AddressableGroup.Plants)]
        public ARAssetReference regrowablePart;
        [Saved] public ARTimeSpan growthTime;
    }
}