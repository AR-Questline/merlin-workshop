using System;
using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Stats.Tweaks;
using Awaken.TG.Main.Skills.Passives;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Passives {
    [TypeIcon(typeof(FlowGraph))]
    [UnitCategory("AR/Skills/Passives")]
    [UnitTitle("Item Type Tweak")]
    [UnityEngine.Scripting.Preserve]
    public class ItemTypeTweakUnit : PassiveSpawnerUnit {
        [Serialize, Inspectable, UnitHeaderInspectable] public int count;
        
        ARValueInput<Func<Item, bool>> _filter;
        ARValueInput<StatType>[] _stats;
        ARValueInput<TweakPriority>[] _types;
        ARValueInput<float>[] _modifiers;
        
        protected override void Definition() {
            _filter = RequiredARValueInput<Func<Item, bool>>("filter");
            _stats = new ARValueInput<StatType>[count];
            _types = new ARValueInput<TweakPriority>[count];
            _modifiers = new ARValueInput<float>[count];
            for (int i = 0; i < count; i++) {
                _stats[i] = RequiredARValueInput<StatType>("stat " + i);
                _types[i] = InlineARValueInput("type " + i, TweakPriority.Add);
                _modifiers[i] = InlineARValueInput("modifier " + i, 0f);
            }
        }

        protected override IPassiveEffect Passive(Skill skill, Flow flow) {
            var filter = _filter.Value(flow);
            var datas = new List<PassiveItemTypeTweak.TweakData>(count);
            for (int i = 0; i < count; i++) {
                var stat = _stats[i].Value(flow);
                var type = _types[i].Value(flow);
                var modifier = _modifiers[i].Value(flow);
                if (stat != null) {
                    datas.Add(new PassiveItemTypeTweak.TweakData(stat, type, modifier));
                }
            }
            return new PassiveItemTypeTweak(filter, datas.ToArray());
        }
    }
}