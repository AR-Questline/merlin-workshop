using System.Collections.Generic;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Utils;
using Awaken.TG.Main.AI.Debugging;
using Awaken.TG.Main.AI.States.CrimeReactions;
using Awaken.TG.Main.AI.States.Flee;
using Awaken.TG.Main.AI.States.ReturnToSpawn;
using Awaken.TG.Main.AI.States.WyrdConversion;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Utility.StateMachines;
using Awaken.TG.MVC;
using Awaken.Utility.Maths;
using Unity.Collections;
using UnityEngine;

namespace Awaken.TG.Main.AI.States {
    public class StateAIWorking : NpcStateMachine<NpcBehaviour> {
        // === Fields
        Perception _perception;
        bool _isFleeing;

        // === Properties
        StateSpawn Spawn { get; } = new();
        StateIdle Idle { get; } = new();
        StateAlert Alert { get; } = new();
        StateCombat Combat { get; } = new();
        StateFlee Flee { get; } = new();
        StateReturn ReturnToSpawn { get; } = new();
        StateReturnAfterVictory ReturnToSpawnAfterVictory { get; } = new();
        StateWyrdConversion WyrdConversion { get; } = new();
        StateCrimeReaction CrimeReaction { get; } = new();
        
        public float AlertValue => AI.AlertValue;
        public bool AreFearfulsInDanger => _perception.AreFearfulsInDanger;
        public Perception Perception => _perception;

        bool DefensiveReturnToSpawnArchetype => Npc.ReturnToSpawnPointArchetype == ReturnToSpawnPointArchetype.Defensive;
        protected override IState InitialState => Spawn.CanEnter ? Spawn : Idle;
        
        protected override void BeforeInit() {
            base.BeforeInit();
            _isFleeing = CrimeReactionUtils.IsFleeing(Npc);
        }

        protected override IEnumerable<IState> States() {
            yield return Spawn;
            yield return Idle;
            yield return Alert;
            yield return Combat;
            yield return Flee;
            yield return ReturnToSpawn;
            yield return ReturnToSpawnAfterVictory;
            yield return WyrdConversion;
            yield return CrimeReaction;
        }

        protected override IEnumerable<StateTransition> Transitions() {
            if (!_isFleeing) {
                yield return new StateTransition(Spawn, Idle, new PollTransition(Spawn2Idle));
                yield return new StateTransition(Idle, Alert, new PollTransition(Idle2Alert));
                yield return new StateTransition(Alert, Idle, new PollTransition(Alert2Idle));
                yield return new StateTransition(null, Combat, new PollTransition(AnyToCombat));
                
                yield return new StateTransition(Combat, (ReturnToSpawnAfterVictory, ReturnToSpawnAfterVictory.TauntFromCombat), ListenTransition.New(Npc, ICharacter.Events.CombatVictory, Combat2SpawnPoint));
                yield return new StateTransition(Combat, (Alert, Alert.Wander), ListenTransition.New(Npc, ICharacter.Events.CombatDisengagement, Combat2AlertInstant));
                
                yield return new StateTransition(Combat, Alert, ListenTransition.New(Npc, ICharacter.Events.CombatExited, Combat2Alert));
                
                if (Npc.ReturnToSpawnPointArchetype is ReturnToSpawnPointArchetype.UseIdleInstant) {
                    yield return new StateTransition(Combat, Idle, ListenTransition.New(Npc, ICharacter.Events.CombatExited, Combat2SpawnPoint));
                    yield return new StateTransition(Combat, Idle, new PollTransition(Combat2SpawnPoint));
                    yield return new StateTransition(Alert, Idle, new PollTransition(Alert2SpawnPoint));
                } else {
                    yield return new StateTransition(Combat, ReturnToSpawn, ListenTransition.New(Npc, ICharacter.Events.CombatExited, Combat2SpawnPoint));
                    yield return new StateTransition(Combat, (ReturnToSpawn, ReturnToSpawn.TauntFromCombat), new PollTransition(Combat2SpawnPoint));
                    yield return new StateTransition(Alert, (ReturnToSpawn, ReturnToSpawn.TauntFromAlert), new PollTransition(Alert2SpawnPoint));
                }

                yield return new StateTransition(ReturnToSpawnAfterVictory, Alert, ListenTransition.New(Npc.HealthElement, HealthElement.Events.OnDamageTaken, VictoriousSpawnPoint2Alert));
                yield return new StateTransition(ReturnToSpawn, Alert, ListenTransition.New(Npc.HealthElement, HealthElement.Events.OnDamageTaken, NotVictoriousSpawnPoint2Alert));
                
                yield return new StateTransition(null, (WyrdConversion, WyrdConversion.WyrdConversionIn), new PollTransition(AnyToWyrdConversionIn));
                yield return new StateTransition(null, (WyrdConversion, WyrdConversion.WyrdConversionOut), new PollTransition(AnyToWyrdConversionOut));
                yield return new StateTransition(WyrdConversion, Idle, new PollTransition(WyrdConversionToIdle));
                yield return new StateTransition(WyrdConversion, Alert, new PollTransition(WyrdConversionToAlert));
                yield return new StateTransition(WyrdConversion, Combat, new PollTransition(WyrdConversionToCombat));
                
            } else {
                yield return new StateTransition(null, Flee, new PollTransition(Any2Flee));
                yield return new StateTransition(Flee, ReturnToSpawn, new PollTransition(Flee2SpawnPoint));
            }
            if (CrimeReactionUtils.IsGuard(Npc)) {
                yield return new StateTransition(Idle, CrimeReaction, new PollTransition(ReactToCrime));
                yield return new StateTransition(Idle, (CrimeReaction, CrimeReaction.PlayerSuspicious), ListenTransition.New(Npc.ParentModel, NpcCrimeReactions.Events.ObservingStateChanged, PlayerActSuspicious));
            }
            
            yield return new StateTransition(CrimeReaction, Alert, new PollTransition(Crime2Alert));
            yield return new StateTransition(CrimeReaction, Idle, new PollTransition(Crime2Idle));
            
            yield return new StateTransition(ReturnToSpawnAfterVictory, Idle, new PollTransition(ReturnToSpawnAfterVictory.EndedDelegate));
            yield return new StateTransition(ReturnToSpawn, Idle, new PollTransition(ReturnToSpawn.EndedDelegate));
            yield return new StateTransition(null, Idle, ListenTransition.New(Npc, ICharacter.Events.ForceEnterStateIdle));
        }

        protected override void OnInit() {
            _perception = new(AI, Data, _isFleeing);
        }

        protected override void OnEnter() {
            base.OnEnter();
            AI.Working = true;
            if (Npc.CanTriggerAggroMusic) {
                World.Only<HeroCombat>().RegisterNearNpcAI(AI);
            }
            NpcAI.AllWorkingAI.Add(AI);

            if (Npc.ReturnToSpawnPointArchetype == ReturnToSpawnPointArchetype.Offensive) {
                Npc.ListenTo(HealthElement.Events.BeforeDamageDealt, OnDamageDealt, this);
            }
            _perception.OnStart(_isFleeing);
        }

        protected override void OnExit() {
            base.OnExit();
            Movement.ResetMainState(null);
            NpcAI.AllWorkingAI.RemoveSwapBack(AI);
            World.Only<HeroCombat>().UnregisterNearNpcAI(AI);
            bool discarded = Npc.HasBeenDiscarded;
            _perception.OnStop(_isFleeing, discarded);
            if (discarded) {
                return;
            }
            AI.AlertStack.Reset();
            AI.HeroVisibility = 0;
            AI.Working = false;
            _perception.OnExit();
        }

        protected override void OnUpdate(float deltaTime) {
            if (!AI.PerceptionUpdateEnabled || deltaTime == 0f) {
                return;
            }
            
            if (_isFleeing) {
                _perception.UpdateFleeing(deltaTime);
            } else {
                _perception.UpdateCombatant(deltaTime);
            }
        }

        // === Transitions
        public bool Spawn2Idle() => Spawn.CanLeave;
        public bool Idle2Alert() => Data.alert.CanEnterAlert && AlertValue >= StateAlert.Idle2LookAtPercentage && Npc.CanEnterCombat(false);
        public bool Alert2Idle() => Alert.CanExitToIdle && AlertValue <= 0f;
        bool Combat2Alert(ICharacter _) => Data.alert.CanEnterAlert && CanLeaveCombat() && (AI.IsOverBand1() || CrimeReactionUtils.IsGuard(Npc));
        bool Combat2AlertInstant(ICharacter _) => Data.alert.CanEnterAlert && Data.alert.CanWander && CanLeaveCombat() && !AI.IsOverBand1() && !AI.HeroVisible && Npc.GetCurrentTarget() == null;
        bool Combat2SpawnPoint(ICharacter character) => CanLeaveCombat() && !Combat2Alert(character);
        bool NotVictoriousSpawnPoint2Alert(DamageOutcome damageOutcome) => VictoriousSpawnPoint2Alert(damageOutcome) && !DefensiveReturnToSpawnArchetype && AI.IsInBand0();
        bool VictoriousSpawnPoint2Alert(DamageOutcome _) => Data.alert.CanEnterAlert && Idle2Alert();
        bool Alert2SpawnPoint() => AI.IsOverBand2();
        bool Combat2SpawnPoint() => CanLeaveCombat() && !Npc.Template.IsPreyAnimal && AI.IsOverBand3();
        public bool AnyToCombat() => !AI.InWyrdConversion && !AI.InSpawn && AI.InCombat;
        bool AnyToWyrdConversionIn() => !AI.InSpawn && AlertValue > 0 && Npc.CanBeWyrdConverted;
        bool AnyToWyrdConversionOut() => !AI.InSpawn && Npc.CanBeOutWyrdConverted && Npc.IsSafeFromWyrdness;
        bool WyrdConversionToIdle() => WyrdConversion.ConversionEnded() && Alert2Idle();
        bool WyrdConversionToAlert() => WyrdConversion.ConversionEnded() && Idle2Alert();
        bool WyrdConversionToCombat() => WyrdConversion.ConversionEnded() && AnyToCombat();
        bool Any2Flee() => !AI.InSpawn && _perception.ShouldFlee;
        bool Flee2SpawnPoint() => !_perception.InAnyDanger;
        
        bool CanLeaveCombat() => !AI.InWyrdConversion && !AI.InSpawn && Combat.CanLeave;

        bool ReactToCrime() => AI.IsInBand2() && Npc.ParentModel.DefaultOwner?.CrimeSavedData.LastCrimeLocationOfInterest != null;
        bool PlayerActSuspicious(bool observationState) => observationState && !Npc.HasElement<BlockEnterCombatMarker>();
        bool PlayerActSuspicious() => Npc.Element<NpcCrimeReactions>().IsObservingHero && !Npc.HasElement<BlockEnterCombatMarker>();
        bool Crime2Idle() => !ReactToCrime() && !PlayerActSuspicious(); // TODO
        bool Crime2Alert() => false; // && Idle2Alert(); TODO

        void OnDamageDealt() {
            Npc.LastOutOfCombatPosition = Npc.Coords;
        }
        
        public override void OnDrawGizmos(AIDebug.Data data) {
        #if UNITY_EDITOR
            base.OnDrawGizmos(data);
            if (AIDebug.DrawViewCone) {
                UnityEditor.Handles.color = Color.blue.WithAlpha(0.2f);
                UnityEditor.Handles.DrawSolidArc(data.elevatedPosition, Vector3.up, data.headForward, Data.perception.MaxAngle(AI), Data.perception.MaxDistance(AI) * Npc.Stat(NpcStatType.SightLengthMultiplier).ModifiedValue * data.visionMultiplier);
                UnityEditor.Handles.DrawSolidArc(data.elevatedPosition, Vector3.up, data.headForward, -Data.perception.MaxAngle(AI), Data.perception.MaxDistance(AI) * Npc.Stat(NpcStatType.SightLengthMultiplier).ModifiedValue * data.visionMultiplier);
            }
        #endif
        }
    }
}