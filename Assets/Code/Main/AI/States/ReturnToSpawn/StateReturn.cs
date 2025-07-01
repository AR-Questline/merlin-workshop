using System;
using System.Collections.Generic;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Utility.StateMachines;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.AI.States.ReturnToSpawn {
    public class StateReturn : NpcStateMachine<StateAIWorking> {
        public StateReturnTauntFromCombat TauntFromCombat { get; } = new();
        public StateReturnTauntFromAlert TauntFromAlert { get; } = new();
        protected virtual StateReturnToSpawnPoint ReturnToSpawnPoint { get; } = new(VelocityScheme.Run);
        StateHealAfterReturn HealAfterReturn { get; } = new();

        bool _returned;
        
        bool ReturnedToSpawnPoint => ReturnToSpawnPoint.Reached;
        
        public Func<bool> EndedDelegate => IsDefensive ? 
            () => _returned && (Npc.Health.IsMax || HealAfterReturn.Healed) : 
            () => _returned;
        ReturnToSpawnPointArchetype Type => Npc.ReturnToSpawnPointArchetype;
        bool IsDefensive => Type == ReturnToSpawnPointArchetype.Defensive;
        bool IsOffensive => Type == ReturnToSpawnPointArchetype.Offensive;
        protected virtual bool RunToSpawn => true;

        protected override IEnumerable<IState> States() {
            yield return TauntFromCombat;
            yield return TauntFromAlert;
            yield return ReturnToSpawnPoint;
            yield return HealAfterReturn;
        }

        protected override IEnumerable<StateTransition> Transitions() {
            yield return new StateTransition(TauntFromAlert, ReturnToSpawnPoint, new PollTransition(AlertTauntToReturn));
            yield return new StateTransition(TauntFromCombat, ReturnToSpawnPoint, new PollTransition(CombatTauntToReturn));
            if (IsDefensive) {
                yield return new StateTransition(ReturnToSpawnPoint, HealAfterReturn, new PollTransition(ReturnToHeal));
            }
        }
        
        protected override void OnEnter() {
            base.OnEnter();
            AI.IsRunningToSpawn = RunToSpawn;
            AI.InReturningToSpawn = true;
            AI.AlertStack.Reset();
            AI.AlertStack.TopDecreaseRate = 2;

            if (IsOffensive) {
                Npc.HealthElement.ListenTo(HealthElement.Events.BeforeDamageTaken, OnDamageTaken, this);
            }

            _returned = false;
        }

        protected override void OnExit() {
            base.OnExit();
            AI.IsRunningToSpawn = false;
            AI.InReturningToSpawn = false;
            
            if (!Npc.HasBeenDiscarded && IsOffensive) {
                Npc.LastOutOfCombatPosition = Npc.Coords;
            }
            
            _returned = false;
        }

        protected override void OnUpdate(float deltaTime) {
            _returned = _returned || ReturnedToSpawnPoint;
        }

        void OnDamageTaken(Damage damage) {
            if (damage.DamageDealer != null) {
                Npc.LastOutOfCombatPosition = Npc.Coords;
                AI.UpdateDistanceToLastIdlePoint();
            }
        }
        
        // --- Transitions
        bool AlertTauntToReturn() => TauntFromAlert.TauntEnded;
        bool CombatTauntToReturn() => TauntFromCombat.TauntEnded;
        bool ReturnToHeal() => _returned && !Npc.Health.IsMax;
    }
}
