using Awaken.TG.Main.AI.Debugging;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility.Maths;
using UnityEngine;

namespace Awaken.TG.Main.AI.States {
    public class StateIdle : NpcState<StateAIWorking> {
        protected override void OnEnter() {
            base.OnEnter();
            AI.InIdle = true;
            Movement.Controller.RefreshRichAIActivity();
            Npc.SetAnimatorState(NpcFSMType.GeneralFSM, NpcStateType.Idle);
        }

        protected override void OnExit() {
            var npc = Npc; // Cache for performance
            if (!npc.HasBeenDiscarded) {
                AI.InIdle = false;
                Movement.Controller.RefreshRichAIActivity();
                npc.LastIdlePosition = npc.LastOutOfCombatPosition = Npc.Coords;
            }
            base.OnExit();
        }

        public override void Update(float deltaTime) {
            var npc = Npc; // Cache for performance
            npc.LastIdlePosition = npc.LastOutOfCombatPosition = npc.Coords;
        }

        public override void OnDrawGizmos(AIDebug.Data data) {
        #if UNITY_EDITOR
            base.OnDrawGizmos(data);
            UnityEditor.Handles.color = Color.green.WithAlpha(0.3f);
            UnityEditor.Handles.DrawSolidDisc(data.elevatedPosition, Vector3.up, Npc.Radius * 2);
        #endif
        }
    }
}