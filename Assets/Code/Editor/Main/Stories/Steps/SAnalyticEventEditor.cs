using Awaken.TG.Editor.Main.Stories.Drawers;
using Awaken.TG.Main.Stories.Steps;

namespace Awaken.TG.Editor.Main.Stories.Steps {
    [CustomElementEditor(typeof(SEditorAnalyticEvent))]
    public class SAnalyticEventEditor : ElementEditor {
        protected override void OnElementGUI() {
            DrawPropertiesExcept("progressionType");
            
            SEditorAnalyticEvent step = Target<SEditorAnalyticEvent>();
            if (step.evtType == SAnalyticEvent.EventType.Progression) {
                DrawProperties("progressionType");
            }
        }
    }
}