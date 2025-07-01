using System;

namespace Awaken.TG.Main.UI.HUD {
    [Flags]
    public enum HUDState {
        None = 0,
        CompassHidden = 1 << 3,
        TutorialsHidden = 1 << 6,
        NotificationsHidden = 1 << 8,
        QuestTrackerHidden = 1 << 9,

        MiddlePanelShown = NotificationsHidden,
        StoryPanel = MiddlePanelShown,
        Spyglass = TutorialsHidden | NotificationsHidden,
        EverythingHidden = CompassHidden | TutorialsHidden | NotificationsHidden | QuestTrackerHidden,
    }
}