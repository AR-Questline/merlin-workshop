using Awaken.TG.Editor.Main.Stories.Drawers;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.FancyPanel;

namespace Awaken.TG.Editor.Main.Stories.Steps {
    [CustomElementEditor(typeof(SEditorFancyPanel))]
    public class SFancyPanelEditor : ElementEditor {

        protected override void OnElementGUI() {
            SEditorFancyPanel sEditorFancyPanel = Target<SEditorFancyPanel>();
            DrawProperties("type");
            if (sEditorFancyPanel.type?.EnumAs<FancyPanelType>()?.UsesText ?? false) {
                DrawProperties("text");
            }
        }
    }
}