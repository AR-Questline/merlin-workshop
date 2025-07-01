using Awaken.TG.Main.UI.Components;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Tooltips;
using TMPro;
using UnityEngine.UI;

namespace Awaken.TG.Main.Settings.Options.Views {
    [UsesPrefab("Settings/VLinkOption")]
    public class VLinkOption : View<Model>, IVSetting, IWithTooltip {

        public ARButton button;
        public TextMeshProUGUI displayName;
        public ARSelectable selectable;
        PrefOption _option;

        public Selectable MainSelectable => selectable;

        public void Setup(PrefOption option) {
            _option = option;
            var linkOption = (LinkOption) option;
            displayName.text = linkOption.DisplayName;
            button.OnClick += linkOption.Callback;
        }

        // === Tooltip
        public UIResult Handle(UIEvent evt) => UIResult.Ignore;
        public TooltipConstructor TooltipConstructor => _option.TooltipConstructor?.Invoke();
    }
}