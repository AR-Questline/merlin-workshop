using System;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("NPC/NPC: Remove status")]
    public class SEditorStatusRemoveNpc : EditorStep {
        public LocationReference locations;
        [TemplateType(typeof(StatusTemplate))] public TemplateReference[] byTemplate = Array.Empty<TemplateReference>();
        [RichEnumExtends(typeof(StatusType))] public RichEnumReference[] byType = Array.Empty<RichEnumReference>();

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SStatusRemoveNpc {
                locations = locations,
                byTemplate = byTemplate,
                byType = byType
            };
        }
    }

    public partial class SStatusRemoveNpc : StoryStepWithLocationRequirement {
        public LocationReference locations;
        public TemplateReference[] byTemplate = Array.Empty<TemplateReference>();
        public RichEnumReference[] byType = Array.Empty<RichEnumReference>();

        protected override LocationReference RequiredLocations => locations;

        protected override DeferredLocationExecution GetStepExecution(Story story) {
            return new StepExecution(byTemplate, byType);
        }

        public partial class StepExecution : DeferredLocationExecution {
            public override ushort TypeForSerialization => SavedTypes.StepExecution_StatusRemoveNpc;

            [Saved] TemplateReference[] _byTemplate = Array.Empty<TemplateReference>();
            [Saved] RichEnumReference[] _byType = Array.Empty<RichEnumReference>();

            [JsonConstructor, Preserve]
            StepExecution() { }

            public StepExecution(TemplateReference[] byTemplate, RichEnumReference[] byType) {
                _byTemplate = byTemplate;
                _byType = byType;
            }

            public override void Execute(Location location) {
                var statuses = location.TryGetElement<ICharacter>()?.Statuses;
                if (statuses == null) {
                    return;
                }

                foreach (var status in _byTemplate.Select(t => t.Get<StatusTemplate>())) {
                    statuses.RemoveAllStatus(status);
                }

                foreach (var statusType in _byType.Select(t => t.EnumAs<StatusType>())) {
                    statuses.RemoveAllStatusesOfType(statusType);
                }
            }
        }
    }
}