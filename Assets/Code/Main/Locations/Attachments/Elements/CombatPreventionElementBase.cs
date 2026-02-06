using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Stats.Tweaks;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public abstract partial class CombatPreventionElementBase : KillPreventionElement<Location> {
        StatTweak _statusResistanceTweak;
        BlockEnterCombatMarker _blockEnterCombatMarker;
        IgnoreEnviroDangerMarker _ignoreEnviroDangerMarker;

        protected override void Init() {
            base.Init();
            if (ParentModel.TryGetElement<NpcElement>(out var npc)) {
                InitElements(npc);
            }
        }

        protected virtual void InitElements(NpcElement npc) {
            AddElements(npc);
        }
        
        protected void AddElements(NpcElement npc) {
            if (_statusResistanceTweak is not { HasBeenDiscarded: false }) {
                _statusResistanceTweak = new StatTweak(npc.Stat(AliveStatType.StatusResistance), 1, TweakPriority.Override, OperationType.Override, this);
            }
            if (_blockEnterCombatMarker is not { HasBeenDiscarded: false }) {
                _blockEnterCombatMarker = npc.AddElement<BlockEnterCombatMarker>();
            }
            if (_ignoreEnviroDangerMarker is not { HasBeenDiscarded: false }) {
                _ignoreEnviroDangerMarker = npc.AddElement<IgnoreEnviroDangerMarker>();
            }
        }

        protected void RemoveElements() {
            if (_statusResistanceTweak is { HasBeenDiscarded: false }) {
                _statusResistanceTweak.Discard();
                _statusResistanceTweak = null;
            }
            if (_blockEnterCombatMarker is { HasBeenDiscarded: false }) {
                _blockEnterCombatMarker.Discard();
                _blockEnterCombatMarker = null;
            }
            if (_ignoreEnviroDangerMarker is { HasBeenDiscarded: false }) {
                _ignoreEnviroDangerMarker.Discard();
                _ignoreEnviroDangerMarker = null;
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            RemoveElements();
            base.OnDiscard(fromDomainDrop);
        }
    }
}