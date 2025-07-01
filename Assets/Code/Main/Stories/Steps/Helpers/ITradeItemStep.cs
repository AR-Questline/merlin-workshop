using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Execution;

namespace Awaken.TG.Main.Stories.Steps.Helpers {
    /// <summary>
    /// Marks steps that should be executed when trade is opened
    /// </summary>
    public interface ITradeItemStep {
        [UnityEngine.Scripting.Preserve] StepResult Execute(Story api);
    }
}