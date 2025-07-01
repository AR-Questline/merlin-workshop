using Awaken.TG.Assets;
using Awaken.TG.Main.UI.Popup.PopupContents;
using JetBrains.Annotations;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Journal.Entries {
    public interface IJournalEntryData {
        string Name { get; }
        string Description { get; }
        string[] UnlockedSubEntries { get; }
        [CanBeNull] ShareableSpriteReference PreviewImage { get; }
        [CanBeNull] DynamicContent DescriptionContent { get; }

        public bool Equals(IJournalEntryData other) {
            return this.Name == other.Name && this.Description == other.Description;
        }
    }
    
    public struct JournalEntryData : IJournalEntryData {
        public string Name { get; }
        public string Description { get; }
        public string[] UnlockedSubEntries { get; }
        public ShareableSpriteReference PreviewImage { get; }
        public DynamicContent DescriptionContent { get; }
        
        public JournalEntryData(string name, string description, string[] unlockedSubEntries, [CanBeNull] ShareableSpriteReference previewImage = null, [CanBeNull] DynamicContent descriptionContent = null) {
            Name = name;
            Description = description;
            PreviewImage = previewImage;
            DescriptionContent = descriptionContent;
            UnlockedSubEntries = unlockedSubEntries;
        }
    }
}