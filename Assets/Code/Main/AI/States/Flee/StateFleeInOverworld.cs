using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Grounds;
using Awaken.TG.MVC;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.AI.States.Flee {
    public class StateFleeInOverworld : NpcState<StateFlee> {
        Wander _wander;
        NpcChunk _destinationChunk;

        public override void Init() {
            base.Init();
            _wander = new Wander(CharacterPlace.Default, VelocityScheme.Run);
            _wander.OnEnd += OnWanderReached;
        }

        protected override void OnEnter() {
            base.OnEnter();
            AttemptToAssignNewDestination();
        }

        void OnWanderReached() {
            Npc.SetAnimatorState(NpcFSMType.GeneralFSM, NpcStateType.Fear);
        }

        public override void Update(float deltaTime) {
            if (!HasSafeDestination()) {
                AttemptToAssignNewDestination();
            }
        }

        bool HasSafeDestination() => _destinationChunk is { Data: { HasDanger: false } };

        void AttemptToAssignNewDestination() {
            if (TryGetNewPosition(out _destinationChunk, out var position)) {
                Npc.SetAnimatorState(NpcFSMType.GeneralFSM, NpcStateType.Idle);
                _wander.UpdateDestination(position, 0.8f);
                Movement.ChangeMainState(_wander);
            } else {
                Movement.ResetMainState(_wander);
            }
        }
        
        protected override void OnExit() {
            base.OnExit();
            _destinationChunk = null;
            Movement.ResetMainState(_wander);
        }

        bool TryGetNewPosition(out NpcChunk safeChunk, out Vector3 safePosition) {
            if (Npc.NpcChunk is { } myChunk) {
                var grid = World.Services.Get<NpcGrid>();
                foreach (var chunk in grid.GetChunksByProximity(myChunk.Coords, 3)) {
                    if (!chunk.Data.HasDanger) {
                        safeChunk = chunk;
                        var chunkCenter = (new float2(chunk.Coords) + new float2(0.5f)) * grid.ChunkSize;
                        safePosition = new Vector3(chunkCenter.x, Npc.Coords.z, chunkCenter.y);
                        safePosition = Ground.SnapToGround(safePosition);
                        return true;
                    }
                }
            }
            safeChunk = null;
            safePosition = default;
            return false;
        }
    }
}