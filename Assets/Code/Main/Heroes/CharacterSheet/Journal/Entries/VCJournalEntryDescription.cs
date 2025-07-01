using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.CharacterSheet.Journal.Content;
using Awaken.TG.Main.Heroes.CharacterSheet.Journal.JournalRecipe;
using Awaken.TG.Main.Heroes.CharacterSheet.Journal.Tabs;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.GameObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Journal.Entries {
    public class VCJournalEntryDescription : ViewComponent {
        const string separator = "<line-height=40><align=\"center\">\n<size=150%>---</size>\n</align></line-height>";
        
        [field: SerializeField] public Transform PreviewImageRoot { get; private set; }
        [field: SerializeField] public Transform DescriptionRoot { get; private set; }
        [field: SerializeField] public Image PreviewImage { get; private set; }
        [field: SerializeField] public TMP_Text TitleText { get; private set; }
        [field: SerializeField] public TMP_Text DescriptionText { get; private set; }
        
        View _currentDescriptionContent;
        SpriteReference _previewSpriteReference;

        protected override void OnAttach() {
            DescriptionRoot.gameObject.SetActive(false);
            PreviewImageRoot.gameObject.SetActive(false);

            World.EventSystem.ListenTo(EventSelector.AnySource, IJournalCategoryUI.Events.EntrySelected, this, Refresh);
            World.EventSystem.ListenTo(EventSelector.AnySource, JournalRecipeUI.Events.CategoryChanged, this, Hide);
        }

        void Hide() {
            DescriptionRoot.gameObject.TrySetActiveOptimized(false);
            PreviewImageRoot.gameObject.TrySetActiveOptimized(false);
        }

        void Refresh(IJournalEntryData entryData) {
            _currentDescriptionContent?.Discard();
            TitleText.SetText(entryData.Name);

            if (entryData.UnlockedSubEntries.Length > 0) {
                DescriptionText.SetText(entryData.Description + separator + string.Join(separator, entryData.UnlockedSubEntries));
            } else {
                DescriptionText.SetText(entryData.Description);
            }
            
            DescriptionRoot.gameObject.SetActive(true);

            bool isUsingPreviewImage = entryData.DescriptionContent is null || 
                                       (entryData.DescriptionContent.ViewContentType != typeof(VJournalTutorialVideoContent) && 
                                       entryData.DescriptionContent.ViewContentType != typeof(VJournalTutorialGraphicContent));
            
            _previewSpriteReference?.Release();
            if (entryData.PreviewImage is { IsSet: true } && isUsingPreviewImage) {
                PreviewImageRoot.gameObject.SetActive(true);
                _previewSpriteReference = entryData.PreviewImage.Get();
                _previewSpriteReference.SetSprite(PreviewImage);
            } else {
                PreviewImageRoot.gameObject.SetActive(false);
            }
            
            if (entryData.DescriptionContent != null) {
                _currentDescriptionContent = World.SpawnView(entryData.DescriptionContent.Element, entryData.DescriptionContent.ViewContentType, true, true, DescriptionText.transform.parent);
            }
        }

        protected override void OnDiscard() {
            _previewSpriteReference?.Release();
            _previewSpriteReference = null;
        }
    }
}
