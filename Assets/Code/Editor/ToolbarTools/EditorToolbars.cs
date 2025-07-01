using UnityEditor;
using UnityEditor.Overlays;

namespace Awaken.TG.Editor.ToolbarTools {
    [Overlay(typeof(SceneView), "ARToolbar", defaultDockPosition = DockPosition.Top, defaultDisplay = true,
        defaultDockZone = DockZone.TopToolbar, defaultDockIndex = -1)]
    public class HeroEditorToolbar : ToolbarOverlay {
        HeroEditorToolbar() : base(HeroFinderToolbarButton.ID, AnimatedLightToolbarButton.ID, AnimatedLightsAllToolbarButton.ID,
            SubscenesToolbarButton.ID, DrawSolidCollidersToolbar.ID) { }
    }
    
    [Overlay(typeof(SceneView), "UIToolbar", defaultDockPosition = DockPosition.Top, defaultDisplay = true,
        defaultDockZone = DockZone.TopToolbar, defaultDockIndex = -1)]
    public class UIEditorToolbar : ToolbarOverlay {
        UIEditorToolbar() : base(UICanvasSelectorToolbarButton.ID, DebugConsoleUIToolbar.ID, UIAspectRatioToolbarButton.ID) { }
    }
    
    [Overlay(typeof(SceneView), "DrakeToolbar", defaultDockPosition = DockPosition.Top, defaultDisplay = true,
        defaultDockZone = DockZone.TopToolbar, defaultDockIndex = -1)]
    public class DrakeEditorToolbar : ToolbarOverlay {
        DrakeEditorToolbar() : base("DrakeHackToolbar", "DrakeHighestLodToolbar") { }
    }
}