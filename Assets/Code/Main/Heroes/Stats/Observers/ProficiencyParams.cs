using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats.Utils;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;

namespace Awaken.TG.Main.Heroes.Stats.Observers {
    [Serializable]
    public struct ProficiencyParams : IStatAndEffectProvider {
        [RichEnumExtends(typeof(ProfStatType))]
        public RichEnumReference proficiency;
        public List<BaseXPParam> baseXPParamsList;
        public float skillUseMult;
        public float skillSpecificMult;
        public float skillUseOffset;
        [ListDrawerSettings(CustomAddFunction = nameof(CustomEffectAdd))]
        public List<StatEffect> effectsList;

        public HeroStatType HeroStat => proficiency.EnumAs<ProfStatType>();
        public IEnumerable<StatEffect> Effects => effectsList;

        // === Helpers
        public float GetBaseXP(BaseXPType typeToFind) {
            if (typeToFind == null) {
                Log.Important?.Error("Trying to get base XP with null type");
                return 0;
            }
            int index = baseXPParamsList.FindIndex(x => x.type == typeToFind);
            if (index == -1) {
                Log.Important?.Error($"Trying to get base XP with type {typeToFind} that is not in the list for {proficiency.Enum.EnumName}");
                return 0;
            }
            return baseXPParamsList[index].xpValue;
        }

        public Stat RetrieveProficiencyFrom(Hero source) => proficiency.EnumAs<ProfStatType>().RetrieveFrom(source);
        public int GetProficiencyLevel(Hero source) => RetrieveProficiencyFrom(source).ModifiedInt;
        public float GetEffectStrOfType(StatType typeToFind) {
            return Effects?.FirstOrDefault(x => x.StatEffected != null && x.StatEffected == typeToFind)?.BaseEffectStrength ?? 0;
        }
        
        public void AttachListeners(Hero target, IListenerOwner listenerOwner) {
            target.ListenTo(Stat.Events.StatChanged(HeroStat), OnStatChanged, listenerOwner);
            OnStatChanged(HeroStat.RetrieveFrom(target));
        }
        
        void OnStatChanged(Stat stat) {
            foreach (var effect in effectsList) {
                effect.RunEffectAtLevel(stat.ModifiedInt, (Hero) stat.Owner);
            }
        }

        // === Odin
        public string ProficiencyName => proficiency?.Enum?.EnumName ?? "";
        
        StatEffect CustomEffectAdd() => new(ProfUtils.GetProfAbstractRefs(proficiency.EnumAs<ProfStatType>()).AbstractTemplate);
        
        [Serializable]
        public struct BaseXPParam {
            [RichEnumExtends(typeof(BaseXPType))]
            public RichEnumReference type;
            public float xpValue;
        }
    }
}