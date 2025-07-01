using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.AI.States.CrimeReactions {
    public class StateInvestigation : NpcState<StateCrimeReaction> {
        Wander _wander;
        public bool DestinationReached { get; private set; }

        public override void Init() {
            base.Init();
            _wander = new Wander(CharacterPlace.Default, VelocityScheme.Run);
        }

        protected override void OnEnter() {
            base.OnEnter();
            CharacterPlace investigationTarget = InvestigationTarget;
            string logInfo = Npc.ParentModel.DebugName + ": Investigating crime: " + investigationTarget.Target + " at " + investigationTarget.Position;
            Log.Debug?.Info(logInfo);
            if (World.Services.Get<DebugAI>()?.ShowThievery == true) {
                DebugAI.SpawnDebugObject(investigationTarget.Position, logInfo);
            }
            _wander.UpdateDestination(investigationTarget);
            Movement.InterruptState(_wander);
            DestinationReached = false;
        }

        public override void Update(float deltaTime) {
            if (Npc.ParentModel.DefaultOwner?.CrimeSavedData.LastCrimeLocationOfInterest == null) {
                DestinationReached = true;
                return;
            }
            var target = InvestigationTarget;
            if (_wander.Destination.DistanceSq(target.Position) > 25) {
                _wander.UpdateDestination(target);
            }
        }
        
        protected override void OnExit() {
            base.OnExit();
            DestinationReached = true;
            Log.Debug?.Info(Npc.ParentModel.DebugName + ": Investigation complete");
        }
        
        // TODO: body investigation
        CharacterPlace InvestigationTarget => new(Npc.ParentModel.DefaultOwner!.CrimeSavedData.LastCrimeLocationOfInterest!.Value, 2);
    }
}