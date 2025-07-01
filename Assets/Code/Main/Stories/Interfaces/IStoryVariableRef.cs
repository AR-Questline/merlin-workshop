using System.Collections.Generic;
using Awaken.TG.Main.Stories.Steps.Helpers;

namespace Awaken.TG.Main.Stories.Interfaces {
    public interface IStoryVariableRef {
        IEnumerable<Variable> variables { get; }
        Context[] optionalContext { get; }
        string extraInfo { get; }
    }
}
