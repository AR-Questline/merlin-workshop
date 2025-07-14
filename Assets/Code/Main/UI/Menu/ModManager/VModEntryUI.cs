using System.Text;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.UI.Menu.ModManager {
    [UsesPrefab("UI/ModManager/" + nameof(VModEntryUI))]
    public class VModEntryUI : RetargetableView<ModEntryUI> {
        [SerializeField] ButtonConfig buttonConfig;
        [SerializeField] ARButton arrowRightButton;
        [SerializeField] ARButton arrowLeftButton;
        [SerializeField] TextMeshProUGUI modNameText;
        [SerializeField] TextMeshProUGUI modeVersionText;
        [SerializeField] TextMeshProUGUI modAuthorText;
        [SerializeField] TextMeshProUGUI modTagsText;
        [SerializeField] TextMeshProUGUI modActiveText;
        
        public ARButton FocusTarget => buttonConfig.button;

        protected override void OnFirstInit() {
            buttonConfig.InitializeButton(OnEntryClicked);
            buttonConfig.button.OnHover += OnHover;
            arrowRightButton.OnClick += ChangeModState;
            arrowLeftButton.OnClick += ChangeModState;
        }

        void OnHover(bool hover) {
            if (!RewiredHelper.IsGamepad) {
                return;
            }

            if (hover && !Target.IsSelected) {
                Target.Select();
            }
        }

        protected override void OnNewTarget() {
            var meta = Target.Metadata;
            modNameText.SetText(meta.name);
            modeVersionText.SetText(meta.version);
            modAuthorText.SetText(meta.author);
            modTagsText.SetText(ExtractTags(meta.tags));
            RefreshSelection();
            RefreshModActiveText();
        }

        static string ExtractTags(string[] tags) {
            StringBuilder sb = new();
            foreach (string t in tags) {
                sb.Append($"#{t}<space=2em>");
            }
            
            return sb.ToString();
        }

        void OnEntryClicked() {
            Target.Select();
        }

        public void RefreshSelection() {
            buttonConfig.SetSelection(Target.IsSelected);
        }

        public void ChangeModState() {
            Target.ToggleActive();
            RefreshModActiveText();
        }
        
        public void RefreshModActiveText() {
            modActiveText.SetText(Target.Active ? LocTerms.On.Translate() : LocTerms.Off.Translate());
        }
    }
}