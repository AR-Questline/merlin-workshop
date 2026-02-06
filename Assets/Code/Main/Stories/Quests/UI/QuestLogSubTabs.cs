using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.SocialServices;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Stories.Quests.UI {
    /// <summary>
    /// Represents and controls quests ui in character sheet 
    /// </summary>
    public partial class QuestLogSubTabs : Tabs<QuestLogRootUI, VQuestLogTabs, QuestLogSubTabType, IQuestLogSubTab> {
        protected override KeyBindings Previous => KeyBindings.UI.Generic.PreviousAlt;
        protected override KeyBindings Next => KeyBindings.UI.Generic.NextAlt;
    }
    
    public interface IQuestLogSubTab : QuestLogSubTabs.ITab { }
    public abstract partial class QuestLogSubTab<TTabView> : QuestLogSubTabs.Tab<TTabView>, IQuestLogSubTab where TTabView : View { }
    
    public class QuestLogSubTabType : QuestLogSubTabs.DelegatedTabTypeEnum {
        [UnityEngine.Scripting.Preserve]
        public static readonly QuestLogSubTabType
            Default = new(nameof(Default), LocTerms.CharacterTabQuestsDefault, _ => new QuestLogUI(QuestCategory.Default), Always),
            Sarras = new(nameof(Sarras), LocTerms.CharacterTabQuestsSarass, _ => new QuestLogUI(QuestCategory.Sarras), questLogRootUI => questLogRootUI.ShowSarrasTab);

        protected QuestLogSubTabType(string enumName, string title, SpawnDelegate spawn, VisibleDelegate visible) : base(enumName, title, spawn, visible) { }
    }
}
