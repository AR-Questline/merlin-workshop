using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Saving.Models;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Pool;

namespace Awaken.TG.Main.Heroes.Stats.Observers {
    [Serializable]
    public class StatEffect {
        [SerializeField, RichEnumExtends(typeof(StatType))] 
        RichEnumReference statEffected;
        [SerializeField, SuffixLabel("$" + nameof(SuffixLabel), true)]
        float effectStrength;
        [SerializeField, RichEnumExtends(typeof(OperationType))] 
        RichEnumReference effectType;
        
        [SerializeField, TemplateType(typeof(ItemTemplate)), 
         ShowIf(nameof(ShowAbstractFilters)), 
         LabelText("$" + nameof(FilterLabel)), 
         InlineButton(nameof(Mode)),
         InlineButton(nameof(Lock))] 
        TemplateReference[] abstractFilters;

        /// <summary>
        /// False => Any<br/> True => All
        /// </summary>
        [SerializeField, HideInInspector] 
        bool allOrAny;

        [SerializeField, ShowIf(nameof(abstractsOverriden)), InlineButton(nameof(Unlock))] 
        bool abstractsOverriden;

        StatTweak _effectTweak;
        ItemTemplate[] _abstractFilters;
        
        // === Public
        
        public StatType StatEffected => statEffected.EnumAs<StatType>();
        public float BaseEffectStrength => effectStrength;
        public OperationType EffectType => effectType.EnumAs<OperationType>();
        public ItemTemplate[] AbstractFilters {
            get {
                if (_abstractFilters == null || _abstractFilters.Length != abstractFilters.Length) {
                    _abstractFilters = abstractFilters.Select(f => f.Get<ItemTemplate>()).ToArray();
                }
                return _abstractFilters;
            }
        }

        public StatEffect(ItemTemplate baseAbstract) {
            if (baseAbstract == null) return;
            abstractFilters = new[] {new TemplateReference(baseAbstract)};
            abstractsOverriden = true;
        }
        
        public float EffectStrength(int level) {
            if (EffectType == OperationType.Multi) {
                return Stat.ToMultiplier(level * effectStrength);
            }

            return level * effectStrength;
        }

        public void RunEffectAtLevel(int level, Hero target) => RunEffectAtLevel(level, target, target.HeroTweaks);

        public void RunEffectAtLevel(int level, IWithStats target, Model targetModel) {
            Stat tweakedStat = target.Stat(StatEffected);
            if (tweakedStat == null) return;
            
            if (_effectTweak == null || _effectTweak.HasBeenDiscarded) {
                _effectTweak = new StatTweak(tweakedStat, 0, null, EffectType, targetModel);
                _effectTweak.MarkedNotSaved = true;
                target.ListenTo(Model.Events.BeforeDiscarded, ReleaseOldDomainReferences);
            }
            _effectTweak.SetModifier(EffectStrength(level));
        }

        void ReleaseOldDomainReferences() {
            _effectTweak = null;
            _abstractFilters = null;
        }

        public bool ShouldApplyToItem(Item target) {
            if (StatEffected is not ItemStatType) return false;
            if (abstractFilters.IsNullOrEmpty()) return true;
            using var allAbstracts = target.Template.AllAbstracts();
            if (!allOrAny) {
                foreach (var filter in AbstractFilters) {
                    if (allAbstracts.value.Contains(filter)) {
                        return true;
                    }
                }

                return false;
            } else {
                foreach (var filter in AbstractFilters) {
                    if (allAbstracts.value.Contains(filter) == false) {
                        return false;
                    }
                }

                return true;
            }
        }
        
        // === Odin: editor only

        string SuffixLabel() {
            if (EffectType == null) return " ";
            return EffectType == OperationType.Multi 
                       ? "Percent     " 
                       : "Flat        ";
        }

        string FilterLabel() {
            if (abstractFilters.IsNullOrEmpty()) return "Abstracts: Require Nothing";
            return allOrAny
                       ? "Abstracts: Require All"
                       : "Abstracts: Require Any";
        }

        void Mode() => allOrAny = !allOrAny;
        void Lock() => abstractsOverriden = true;
        void Unlock() => abstractsOverriden = false;
        bool ShowAbstractFilters => StatEffected is ItemStatType && !abstractsOverriden;
    }
}