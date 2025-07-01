using Awaken.Utility;
using System.Collections.Generic;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Unity.Collections;

namespace Awaken.TG.Main.Memories.Journal.Conditions.Models {
    public partial class KillCountRuntime : ConditionRuntime {
        public override ushort TypeForSerialization => SavedModels.KillCountRuntime;

        readonly List<KillCountCondition> _conditions = new(10);
        [Saved] Dictionary<JournalGuid, int> _killCounts = new(10);

#if UNITY_EDITOR
        // TODO: remove once journal is implemented and tested
        List<NpcElement>[] _EDITOR_trackedNPCS {
            get {
                var result = new List<NpcElement>[_conditions.Count];
                for (var i = 0; i < _conditions.Count; ++i) {
                    var condition = _conditions[i];
                    var npcsForCondition = new List<NpcElement>(condition.KillCount);
                    foreach (var npc in World.All<NpcElement>()) {
                        if (condition.AppliesTo(npc)) {
                            npcsForCondition.Add(npc);
                        }
                    }
                    result[i] = npcsForCondition;
                }

                return result;
            }
        }
#endif

        public void RegisterCondition(KillCountCondition condition) {
            _conditions.Add(condition);
        }

        public void UnregisterCondition(KillCountCondition condition) {
            int index = _conditions.IndexOf(condition);
            _conditions.RemoveAtSwapBack(index);
            _killCounts.Remove(condition.Guid);
        }

        public void OnNpcDeath(DamageOutcome damageOutcome) {
            if (damageOutcome.Target is not NpcElement { WasLastDamageFromHero: true } npc || npc.Template == null) {
                return;
            }

            for (int i = _conditions.Count - 1; i >= 0; i--) {
                var condition = _conditions[i];
                if (!condition.AppliesTo(npc)) {
                    continue;
                }

                OnDeath(condition);
            }
        }
        
        void OnDeath(KillCountCondition condition) {
            if (!_killCounts.TryGetValue(condition.Guid, out int kills)) {
                kills = 1;
            } else {
                ++kills;
            }

            _killCounts[condition.Guid] = kills;

            condition.OnKill(kills, this);
        }
    }
}