using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Utility.Attributes.List;
using Awaken.Utility.Enums;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Statuses {
    public class StatusConvertConfig : ScriptableObject {
        [List(ListEditOption.Buttons)]
        public List<ConvertGroup> groups;

        [UnityEngine.Scripting.Preserve]
        public IEnumerable<StatusTemplate> ConvertStatuses(Status[] fightStatuses) {
            foreach (StatusType type in RichEnum.AllValuesOfType<StatusType>()) {
                int factorSum = fightStatuses.Where(s => s.Type == type).Sum(s => s.Template.fightValueFactor);
                StatusTemplate template = FindTemplateForFactor(type, factorSum);
                if (template != null) {
                    yield return template;
                }
            }
        }

        StatusTemplate FindTemplateForFactor(StatusType type, int factor) {
            ConvertGroup group = groups.FirstOrDefault(g => g.type.EnumAs<StatusType>() == type);
            ConvertItem validItem = group?.items.FirstOrDefault(i => i.factorRange.Contains(factor));
                return validItem?.mapStatus.Get<StatusTemplate>();
        }
        
        [Serializable]
        public class ConvertGroup {
            [RichEnumExtends(typeof(StatusType))]
            public RichEnumReference type;
            public List<ConvertItem> items = new List<ConvertItem>();
        }

        [Serializable]
        public class ConvertItem {
            public IntRange factorRange;
            [TemplateType(typeof(StatusTemplate))]
            public TemplateReference mapStatus;
        }
    }
}