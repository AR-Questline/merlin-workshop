using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.RichEnums;
using Sirenix.OdinInspector;

namespace Awaken.TG.Main.General.Configs {
    [Serializable]
    public class PriceMultiplierConfig {
        [TemplateType(typeof(ItemTemplate))]
        public TemplateReference[] abstracts;
        public bool onlyForGivenQuality;
        
        [RichEnumExtends(typeof(ItemQuality)), ShowIf(nameof(onlyForGivenQuality))]
        public RichEnumReference quality;
        
        public float multiplier;
        
        public ItemQuality Quality => onlyForGivenQuality ? quality.EnumAs<ItemQuality>() : null;

        public bool IsMatching(ItemTemplate template) {
            var reqQuality = Quality;
            if(reqQuality != null && reqQuality != template.Quality) {
                return false;
            }

            var abstractsCount = abstracts.Length;
            using var abstractTypes = template.AbstractTypes;
            for (int i = 0; i < abstractsCount; i++) {
                if (abstractTypes.value.Contains(abstracts[i].Get<ItemTemplate>())) {
                    return true;
                }
            }
            return false;
        }
    }
}