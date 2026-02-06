using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace Awaken.TG.Editor.Debugging.GUIDSearching {
    public class GUIDSearchWindowBase : OdinEditorWindow {
        const string OtherGUIDToolsGroup = "Other GUID Tools";
        const string OtherGUIDToolsButtonsGroup = OtherGUIDToolsGroup+"/Buttons";
        protected virtual bool ShowGUIDSearchButton => true;
        protected virtual bool ShowUnusedSearchButton => true;
        protected virtual bool ShowRichEnumSearchButton => true;
        protected virtual bool ShowIdOverrideSearchButton => true;
        protected virtual bool ShowAlwaysLoadedSearchButton => true;
        
        [HorizontalGroup(OtherGUIDToolsButtonsGroup), PropertyOrder(-1)]
        [Button(ButtonSizes.Small), ShowIf(nameof(ShowGUIDSearchButton))]
        void OpenGUIDSearchWindow() {
            GUIDSearchWindow.OpenWindow();
        }
        
        [BoxGroup(OtherGUIDToolsGroup), HorizontalGroup(OtherGUIDToolsButtonsGroup), PropertyOrder(-1)]
        [Button(ButtonSizes.Small), ShowIf(nameof(ShowUnusedSearchButton))]
        void OpenUnusedSearchWindow() {
            UnusedSearchWindow.OpenWindow();
        }
        
        [HorizontalGroup(OtherGUIDToolsButtonsGroup), PropertyOrder(-1)]
        [Button(ButtonSizes.Small), ShowIf(nameof(ShowRichEnumSearchButton))]
        void OpenRichEnumSearchWindow() {
            RichEnumSearchWindow.OpenWindow();
        }
        
        [HorizontalGroup(OtherGUIDToolsButtonsGroup), PropertyOrder(-1)]
        [Button(ButtonSizes.Small), ShowIf(nameof(ShowIdOverrideSearchButton))]
        void OpenIdOverrideSearchWindow() {
            IdOverrideSearchWindow.OpenWindow();
        }
        
        [HorizontalGroup(OtherGUIDToolsButtonsGroup), PropertyOrder(-1)]
        [Button(ButtonSizes.Small), ShowIf(nameof(ShowAlwaysLoadedSearchButton))]
        void OpenAlwaysLoadedSearchWindow() {
            AlwaysLoadedSearchWindow.OpenWindow();
        }
    }
}