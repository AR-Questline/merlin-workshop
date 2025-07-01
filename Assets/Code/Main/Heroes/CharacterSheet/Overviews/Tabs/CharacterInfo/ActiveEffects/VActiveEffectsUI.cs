using Awaken.TG.Main.Localization;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.CharacterInfo.ActiveEffects {
    [UsesPrefab("CharacterSheet/Overview/VActiveEffectsUI")]
    public class VActiveEffectsUI : View<ActiveEffectsUI>, IVEntryParentUI {
        [SerializeField] Transform effectsParent;
        [SerializeField] TextMeshProUGUI activeEffectsTitleLabel;
        [SerializeField, LocStringCategory(Category.UI)] LocString activeEffectsTitle;

        public Transform EntriesParent => effectsParent;

        public override Transform DetermineHost() => Target.ParentModel.View<VCharacterInfoUI>().ActiveEffectsParent;

        protected override void OnInitialize() {
            activeEffectsTitleLabel.SetText(activeEffectsTitle);
        }

        protected override void OnMount() {
            Target.InitializeEffectEntries();
        }
    }
}