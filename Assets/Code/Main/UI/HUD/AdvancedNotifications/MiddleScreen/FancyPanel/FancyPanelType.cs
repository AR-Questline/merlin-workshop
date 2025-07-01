using Awaken.TG.Main.Localization;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.FancyPanel {
    public class FancyPanelType : RichEnum {
        public delegate void NotificationSpawn(IModel owner, string text = null);
        public NotificationSpawn Spawn { get; }
        public bool UsesText { get; }

        [UnityEngine.Scripting.Preserve]
        public static readonly FancyPanelType
            Custom = new(nameof(Custom)),
            FastTravelUnlocked = new(nameof(FastTravelUnlocked), NotificationWithText(LocTerms.FastTravelUnlockedNotification), false),
            LearnedWyrdSkill = new(nameof(LearnedWyrdSkill), NotificationWithText(LocTerms.LearnedWyrdSkillNotification), false),
            NewJournalEntry = new(nameof(NewJournalEntry), NotificationWithText(LocTerms.NewJournalEntry), false),
            Good = new(nameof(Good), NotificationWithText(LocTerms.Good), false),
            Bad = new(nameof(Bad), NotificationWithText(LocTerms.Bad), false);
        
        
        FancyPanelType(string enumName, NotificationSpawn spawnNotification = null, bool usesText = true) : base(enumName) {
            Spawn = spawnNotification ?? FullCustom;
            UsesText = usesText;
        }
        
        static void FullCustom(IModel relatedModel, string text) {
            AdvancedNotificationBuffer.Push<MiddleScreenNotificationBuffer>(new FancyPanelNotification(text, typeof(VFancyPanelNotification)));
        }

        static NotificationSpawn NotificationWithText(string locTerm) {
            return (owner, _) => {
                FullCustom(owner, locTerm.Translate());
            };
        }
    }
}
