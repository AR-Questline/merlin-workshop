using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Localization;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;

namespace Awaken.TG.Main.Heroes.Fishing {
    [Serializable]
    public partial struct FishEntry {
        public ushort TypeForSerialization => SavedTypes.FishEntry;
        
        [Saved] ItemTemplate _fishTemplate;
        [Saved] public float weight;
        [Saved] public float length;
        
        public ShareableSpriteReference Graphic => _fishTemplate.IconReference();
        public string Id => _fishTemplate.GUID;
        public string Name => _fishTemplate.ItemName;

        public FishEntry(ItemTemplate fishTemplate, float length, float weight) {
            _fishTemplate = fishTemplate;
            this.length = length;
            this.weight = weight;
        }
        
        public string DescriptionToDisplay() {
            string l = length.ToString("0.0").Bold().WithSprite("fish", ARColor.MainGrey);
            string w = weight.ToString("0.0").Bold().WithSprite("weight", ARColor.MainGrey);
            string pipe = string.Empty.WithSprite("pipe", ARColor.MainGrey);
            return $"{LocTerms.FishCurrentRecordJournal.Translate($"{l}")} {pipe} {w} {LocTerms.KilogramAbbreviation.Translate()}";
        }
    }
}