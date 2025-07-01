using Awaken.TG.Main.Templates;

namespace Awaken.TG.Main.Stories.Interfaces {
    public interface IStoryQuestRef {
        TemplateReference QuestRef { get; }
        string TargetValue { get; }
    }
}