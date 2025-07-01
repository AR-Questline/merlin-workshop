using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Maps.Compasses;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Main.AI.States.Flee {
    public class StateFleeInAdditive : NpcState<StateFlee> {
        Wander _wander;

        public override void Init() {
            base.Init();
            _wander = new Wander(CharacterPlace.Default, VelocityScheme.Run);
            _wander.OnEnd += OnReachPortal;
        }

        protected override void OnEnter() {
            base.OnEnter();
            Npc.SetAnimatorState(NpcFSMType.GeneralFSM, NpcStateType.Idle);
            
            if (TryGetFleePositionInAdditiveScene(out var position)) {
                _wander.UpdateDestination(position, 0.8f);
                Movement.ChangeMainState(_wander);
            } else {
                Log.Critical?.Error($"{Npc} cannot find portal to flee", Npc.ParentTransform);
            }
        }

        protected override void OnExit() {
            base.OnExit();
            Movement.ResetMainState(_wander);
        }

        public override void Update(float deltaTime) { }

        bool TryGetFleePositionInAdditiveScene(out Vector3 position) {
            var portal = Portal.FindClosestExit(Npc.Coords);
            if (portal == null) {
                position = default;
                return false;
            } else {
                position = Ground.SnapToGround(portal.ParentModel.Coords);
                return true;
            }
        }

        void OnReachPortal() {
            Movement.Controller.MoveToAbyss();
            Npc.AddElement<ChangeSceneHideCompassMarker>();
        }
    }
}