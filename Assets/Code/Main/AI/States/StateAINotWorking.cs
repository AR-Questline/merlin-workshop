using Awaken.TG.Main.AI.States.ReturnToSpawn;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Utility.StateMachines;
using Awaken.TG.MVC;
using Awaken.Utility;

namespace Awaken.TG.Main.AI.States {
    public class StateAINotWorking : EmptyState<NpcBehaviour> {
        bool _checkTeleport;

        public override void Init() {
            Parent.Npc.ListenTo(NpcAI.Events.NpcStateChanged, OnStateChanged, Parent.AI);
        }

        protected override void OnEnter() {
            TryTeleportToLastIdlePoint();
        }

        // This is a bit hacky way to fix AI position without having "light AI logic"
        void TryTeleportToLastIdlePoint() {
            if (!_checkTeleport || Parent.AI.GetDistanceToLastIdlePointBand() == 0) {
                _checkTeleport = false;
                return;
            }
            _checkTeleport = false;
            Parent.Npc.Movement?.Controller.DisableFallDamageForTeleport();
            Parent.Npc.ParentModel.SafelyMoveTo(Parent.Npc.LastIdlePosition, true);
        }

        void OnStateChanged(Change<IState> stateChange) {
            // This mean: "AI was in StateReturnToSpawnPoint state, but parent state changed"
            if (stateChange is (StateReturn, null) && !NpcPresence.InAbyss(Parent.Npc.ParentModel.Coords)) {
                _checkTeleport = true;
            }
        }
    }
}
