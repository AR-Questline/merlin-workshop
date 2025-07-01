using System;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Stories.Core {
    [Serializable]
    public class VariableReferenceDefine {
        public string name;
        public TemplateReference template;

        public string GetName() {
            if (!template.IsSet) {
                return null;
            }
            var named = TemplatesUtil.Load<ITemplate>(template.GUID) as INamed;
            return named?.DisplayName;
        }

        public bool IsValid() {
            if (!template.IsSet) {
                return false;
            }

            return TemplatesUtil.Load<ITemplate>(template.GUID) is INamed;
        }
    }
}