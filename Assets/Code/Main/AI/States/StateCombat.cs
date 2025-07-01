using Awaken.CommonInterfaces.Animations;
using Awaken.TG.Main.AI.Combat;
using Awaken.TG.Main.AI.Debugging;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.Utility.Maths;
using UnityEngine;

namespace Awaken.TG.Main.AI.States {
    public class StateCombat : NpcState<StateAIWorking>, IAnimatorBridgeStateProvider {
        /// <summary>
        /// radius is lower outside of combat so that NPC's don't block each other as much
        /// </summary>
        public const int AICombatRadiusScale = 2;
        
        public bool AlwaysAnimate => true;
        public bool CanLeave => !HeroCombat.ForceCombat && IsCombatExitAllowed();
        
        AnimatorBridge _npcAnimator;

        protected override void OnEnter() {
            base.OnEnter();
            NpcController npcController = Npc.Movement.Controller;
            npcController.RichAI.radius *= AICombatRadiusScale;
            _npcAnimator = AnimatorBridge.GetOrAddDefault(npcController.Animator);
            _npcAnimator.RegisterStateProvider(this);
        }

        protected override void OnExit() {
            base.OnExit();

            if (_npcAnimator != null) {
                _npcAnimator.UnregisterStateProvider(this);
                _npcAnimator = null;
            }

            if (Npc.HasBeenDiscarded) {
                return;
            }
            
            Npc.Movement.Controller.RichAI.radius /= AICombatRadiusScale;
            AI.ExitCombat(true);
        }

        public override void Update(float deltaTime) {}

        bool IsCombatExitAllowed() {
            foreach (var blocker in Npc.Elements<INpcCombatLeaveBlocker>()) {
                if (blocker.BlocksCombatExit) {
                    return false;
                }
            }
            return true;
        }

        public override void OnDrawGizmos(AIDebug.Data data) {
        #if UNITY_EDITOR
            base.OnDrawGizmos(data);
            UnityEditor.Handles.color = Color.red.WithAlpha(0.3f);
            UnityEditor.Handles.DrawSolidDisc(data.elevatedPosition, Vector3.up, Npc.Radius * 2);
            var target = Npc.GetCurrentTarget();
            if (target != null) {
                UnityEditor.Handles.color = Color.red;
                UnityEditor.Handles.DrawLine(Npc.Head.position, target.Torso.position);
            }
        #endif
        }
    }
}