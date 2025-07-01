using System.Collections.Generic;
using Awaken.TG.Main.AI.Debugging;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Duels;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Utility.StateMachines;
using Awaken.TG.MVC;
using Awaken.Utility.Maths;
using UnityEngine;

namespace Awaken.TG.Main.AI.States {
    public class StateAlert : NpcStateMachine<StateAIWorking> {
        public const int Idle2LookAtPercentage = 5;
        public const int LookAt2WanderPercentage = 40;
        public const int Alert2Combat = 90;
        public const int PatrolWalk2RunPercentage = 80;
        public const int PatrolWithWeaponsPercentage = 90;
        
        StateAlertLookAt LookAt { get; } = new();
        public StateAlertWander Wander { get; } = new();
        StateAlertPatrol Patrol { get; } = new();

        StateAlertExit AlertExit { get; } = new();
        public bool CanExitToIdle { get; set; }
        
        Item EquippedItem => Npc.Inventory.EquippedItem(EquipmentSlotType.MainHand)
                             ?? Npc.Inventory.EquippedItem(EquipmentSlotType.OffHand);
        [UnityEngine.Scripting.Preserve] public bool WalkWithWeapons => 
            AI.InAlertWithWeapons || EquippedItem != null || AI.AlertValue > PatrolWithWeaponsPercentage;
        
        protected override IEnumerable<IState> States() {
            yield return LookAt;
            yield return Wander;
            yield return Patrol;
            yield return AlertExit;
        }

        protected override IEnumerable<StateTransition> Transitions() {
            yield return new StateTransition(LookAt, Wander, new PollTransition(LookAt2Wander));
            yield return new StateTransition(Wander, Patrol, new PollTransition(Wander2Patrol));
            yield return new StateTransition(Patrol, Wander, ListenTransition.New(Npc.NpcAI.AlertStack, AlertStack.Events.AlertChanged, Patrol2Wander));
            
            yield return new StateTransition(AlertExit, LookAt, new PollTransition(Exit2LookAt));
            yield return new StateTransition(LookAt, AlertExit, new PollTransition(Any2Exit));
            yield return new StateTransition(Wander, AlertExit, new PollTransition(Any2Exit));
            yield return new StateTransition(Patrol, AlertExit, new PollTransition(Any2Exit));
        }

        protected override void OnEnter() {
            CanExitToIdle = false;
            AI.InAlert = true;
            AI.AlertStack.ListenTo(AlertStack.Events.AlertChanged, OnAlertChanged, this);
            //AI.InAlertWithWeapons = false;
            InformOthers();
            base.OnEnter();
        }
        
        void OnAlertChanged() {
            AI.TryEnterCombatWithHero();
        }

        protected override void OnExit() {
            base.OnExit();
            CanExitToIdle = true;
            if (!Npc.HasBeenDiscarded) {
                AI.InAlert = false;
                //AI.InAlertWithWeapons = false;
                AI.ObserveAlertTarget = false;
            }
        }

        bool LookAt2Wander() => !Data.alert.CanLookAt || (Data.alert.CanWander && !Npc.NpcAI.AlertStack.AlertTransitionsPaused && Parent.AlertValue > LookAt2WanderPercentage);
        bool Wander2Patrol() => Wander.Reached;
        bool Patrol2Wander(AlertStack _) => true;
        bool Exit2LookAt() => Data.alert.CanLookAt && Parent.AlertValue > LookAt2WanderPercentage;
        bool Any2Exit() => !Npc.NpcAI.AlertStack.AlertTransitionsPaused && Parent.AlertValue <= 0f;

        void InformOthers() {
            if (Npc.HasElement<DuelistElement>()) {
                return;
            }
            
            var alertRange = World.Services.Get<GameConstants>().AlertEnterInformRange;
            var targetPos = AI.AlertTarget;
            foreach (NpcElement npc in World.Services.Get<NpcGrid>().GetHearingNpcs(Npc.Coords, alertRange)) {
                if (npc is not { NpcAI: { InIdle: true } ai } || npc == Npc || !npc.IsFriendlyTo(Npc)) {
                    continue;
                }
                ai.AlertStack.NewPoi(Idle2LookAtPercentage, targetPos);
            }
        }
        
        public override void OnDrawGizmos(AIDebug.Data data) {
        #if UNITY_EDITOR
            base.OnDrawGizmos(data);
            
            UnityEditor.Handles.color = Color.yellow.WithAlpha(0.3f);
            //UnityEditor.Handles.DrawSolidArc(data.elevatedPosition, Vector3.up, data.forward, 360 * Parent.Aggro / Data.alert.Combat.max, Npc.Radius * 2);
            //UnityEditor.Handles.DrawWireArc(data.elevatedPosition, Vector3.up, data.forward, 360 * Parent.Aggro / Data.alert.Combat.max, Npc.Radius * 2);
        #endif
        }
    }
}