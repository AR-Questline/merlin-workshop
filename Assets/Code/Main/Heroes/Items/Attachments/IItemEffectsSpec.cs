using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Skills;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    public interface IItemEffectsSpec : IAttachmentSpec {
        ItemActionType ActionType { get; }
        IEnumerable<SkillReference> Skills { get; }
        IEnumerable<SkillReference> SkillRefsFromSpec(object debugTarget) =>
            Skills.Where(s => s?.SkillGraph(debugTarget) != null);
        bool CanBeCharged { get; }
        bool ConsumeOnUse { get; }
        int MaxChargeSteps { get; }
    }
}