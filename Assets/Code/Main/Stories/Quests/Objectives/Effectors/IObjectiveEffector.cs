using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Effectors {
    /// <summary>
    /// Used to execute some code on objective state change
    /// </summary>
    public interface IObjectiveEffector : IElement<Objective> {
        void OnStateUpdate(QuestUtils.ObjectiveStateChange stateChange);
    }
}