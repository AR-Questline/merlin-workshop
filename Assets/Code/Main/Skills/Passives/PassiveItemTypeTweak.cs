using System;
using System.Collections.Generic;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Stats.Tweaks;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Relations;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Skills.Passives {
    public partial class PassiveItemTypeTweak : Element<Skill>, IPassiveEffect {
        public sealed override bool IsNotSaved => true;

        readonly Func<Item, bool> _filter;
        readonly TweakData[] _datas;
        readonly Dictionary<Item, Tweak[]> _tweaksByItem = new();

        public PassiveItemTypeTweak(Func<Item, bool> filter, TweakData[] datas) {
            _filter = filter;
            _datas = datas;
        }

        protected override void OnInitialize() {
            var owner = ParentModel.Owner;
            owner.ListenTo(IItemOwner.Relations.Owns.Events.AfterAttached, OnItemAdded, this);
            owner.ListenTo(IItemOwner.Relations.Owns.Events.BeforeDetached, OnItemRemoved, this);
            if (owner == Hero.Current) {
                Hero.Current.ListenTo(Hero.Events.HeroPerspectiveChanged, Toggle, this);
            }
            Enable();
        }

        void Toggle() {
            if (HasBeenDiscarded) {
                return;
            }
            Disable();
            Enable();
        }
        
        void Enable() {
            foreach (var item in ParentModel.Owner.Inventory.Items) {
                OnItemAdded(item);
            }
        }
        
        void Disable() {
            var tweakSystem = World.Services.Get<TweakSystem>();
            foreach (var tweaks in _tweaksByItem.Values) {
                foreach (var tweak in tweaks) {
                    if (tweak != null) {
                        tweakSystem.RemoveTweak(tweak);
                    }
                }
            }
            _tweaksByItem.Clear();
        }

        void OnItemAdded(RelationEventData data) {
            OnItemAdded((Item)data.to);
        }

        void OnItemAdded(Item item) {
            if (!_filter(item)) return;
            if (_tweaksByItem.ContainsKey(item)) return;

            var tweaks = new Tweak[_datas.Length];
            for (int i = 0; i < _datas.Length; i++) {
                var stat = item.Stat(_datas[i].statType);
                if (stat == null) {
#if AR_DEBUG
                    throw new Exception($"Item {LogUtils.GetDebugName(item)} does not have stat of type {_datas[i].statType.EnumName}");
#endif
                    Log.Important?.Error($"Item {LogUtils.GetDebugName(item)} does not have stat of type {_datas[i].statType.EnumName}");
                    continue;
                }
                tweaks[i] = World.Services.Get<TweakSystem>().Tweak(stat, _datas[i], _datas[i].type);
            }
            _tweaksByItem.Add(item, tweaks);
        }
        
        void OnItemRemoved(RelationEventData data) {
            OnItemRemoved((Item)data.to);
        }
        
        void OnItemRemoved(Item item) {
            if (_tweaksByItem.Remove(item, out var tweaks)) {
                foreach (var tweak in tweaks) {
                    World.Services.Get<TweakSystem>().RemoveTweak(tweak);
                }
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            Disable();
            
        }

        public class TweakData : ITweaker {
            public readonly StatType statType;
            public readonly TweakPriority type;
            public readonly float value;
            
            public OperationType OperationType { get; private set; }

            public TweakData(StatType statType, TweakPriority type, float value) {
                this.statType = statType;
                this.type = type;
                this.value = value;
                
                OperationType = OperationType.GetDefaultOperationTypeFor(type);
            }

            float ITweaker.TweakFn(float original, Tweak tweak) => OperationType.Calculate(original, value);
        }
    }
}