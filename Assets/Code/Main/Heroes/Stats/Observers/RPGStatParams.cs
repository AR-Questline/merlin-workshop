using System;
using System.Collections.Generic;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Stats.Observers {
    [Serializable]
    public struct RPGStatParams : IStatAndEffectProvider {
        [SerializeField, PropertySpace, HorizontalGroup, RichEnumExtends(typeof(HeroRPGStatType))]
        RichEnumReference rpgStat;
        [SerializeField] 
        int innateStatLevel;
        [SerializeField]
        StatEffect[] effectsList;

        public HeroRPGStatType RPGStat => rpgStat.EnumAs<HeroRPGStatType>();
        public HeroStatType HeroStat => RPGStat;
        public int InnateStatLevel => innateStatLevel;
        public IEnumerable<StatEffect> Effects => effectsList;

        public void AttachListeners(Hero target, IListenerOwner listenerOwner) {
            target.ListenTo(Stat.Events.StatChanged(RPGStat), OnStatChanged, listenerOwner);
            OnStatChanged(RPGStat.RetrieveFrom(target));
        }
        
        void OnStatChanged(Stat stat) {
            foreach (var effect in effectsList) {
                effect.RunEffectAtLevel(stat.ModifiedInt, (Hero) stat.Owner);
            }
        }
    }
}