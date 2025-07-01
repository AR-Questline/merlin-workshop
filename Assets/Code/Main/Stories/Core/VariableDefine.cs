using System;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Sirenix.OdinInspector;

namespace Awaken.TG.Main.Stories.Core {
    [Serializable]
    public class VariableDefine {
        public string name;
        [Getter, ForceType(VariableType.Const, VariableType.Defined, VariableType.CurrentDay, VariableType.CurrentWeek)]
        public Variable defaultValue;
        
        [HideLabel]
        public string[] context;
        public Context[] contexts;

        public VariableHandle Create(Story story, float varValue = 0f) {
            Variable variable = new Variable {name = name, type = VariableType.Custom, value = varValue};
            VariableHandle handle = variable.Prepare(story, context, contexts);

            if (!handle.HasValue()) {
                float value = defaultValue.GetValue(story);
                handle.SetValue(value);
            }

            return handle;
        }
    }
}