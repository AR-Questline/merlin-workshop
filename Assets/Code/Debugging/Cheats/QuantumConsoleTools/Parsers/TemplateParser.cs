using System;
using System.Linq;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using QFSW.QC;

namespace Awaken.TG.Debugging.Cheats.QuantumConsoleTools.Parsers {
    [UnityEngine.Scripting.Preserve]
    public class TemplateParser : PolymorphicQcParser<ITemplate> {
        public override int Priority => 10000;

        public override ITemplate Parse(string value, Type type) {
            return World.Services.Get<TemplatesProvider>().GetAllOfType(type, TemplateTypeFlag.All).FirstOrDefault(t => t.DebugName == value);
        }
    }
}
