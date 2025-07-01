using UnityEngine;

namespace Awaken.TG.Debugging.ModelsDebugs {
    public interface IModelsDebugNavigator {
        string SearchString { get; set; }
        
        void Draw(Rect rect);
        MembersList GetSelected();
        void RefreshNavigation();
        bool TrySelectModel(string modelID);
        void Frame();
    }
}