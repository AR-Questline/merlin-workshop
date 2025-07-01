using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.Controllers.Rotation;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Heroes;
using Awaken.Utility.Debugging;
using Awaken.Utility.Maths;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.AI.States.CrimeReactions {
    /// <summary>
    /// Stealing, pickpocketing, crouching
    /// </summary>
    public class StatePlayerSuspicious : NpcState<StateCrimeReaction> {
        const int GuardFollowDistance = 30;
        const int RegularFollowDistance = 1;
        const float ExtraDistanceForFollowing = 4;
        
        const float DistanceForTrot = 10;
        const float DelayToReactToHeroAgain = 1.5f;

        FollowMovement _followMovement;
        NoMoveAndRotateTowardsCustomTarget _rotateTowardsPlayer;
        Wander _returnToStart;
        
        bool _isPeasant;
        float _followDistance;
        float _followAttemptSqr;
        Vector3 _startPoint;
        float _delayToReactToHeroAgain;
        
        NpcCrimeReactions _npcCrimeReactions;
        
        CrimeReactionArchetype CrimeReactionArchetype => Npc.Template.CrimeReactionArchetype;
        
        public override void Init() {
            // guard has a longer follow distance
            // fleeing only looks at the player direction
            _followDistance = Npc.Template.CrimeReactionArchetype == CrimeReactionArchetype.Guard
                ? GuardFollowDistance
                : RegularFollowDistance;
            _followAttemptSqr = math.square(_followDistance + ExtraDistanceForFollowing);
            _isPeasant = CrimeReactionArchetype is CrimeReactionArchetype.FleeingPeasant or CrimeReactionArchetype.AlwaysFleeing;
            _npcCrimeReactions = Npc.Element<NpcCrimeReactions>();
        }

        protected override void OnEnter() {
            _startPoint = Npc.Coords;
            
            _rotateTowardsPlayer = new NoMoveAndRotateTowardsCustomTarget(Hero.Current);
            _returnToStart = new Wander(new(_startPoint, 0.5f), VelocityScheme.Walk);
            _returnToStart.OnEnd += StopInterrupting;
            _followMovement = new FollowMovement(Hero.Current, DistanceForTrot, _startPoint, _followDistance);
            _followMovement.OnEnd += OnStopFollowing;
            
            Movement.ChangeMainState(_rotateTowardsPlayer);

            base.OnEnter();
        }

        void StopInterrupting() => Movement.StopInterrupting();

        void OnStopFollowing() {
            Movement.InterruptState(_returnToStart);
            _delayToReactToHeroAgain = DelayToReactToHeroAgain;
        }

        void StartFollowing() {
            Movement.InterruptState(_followMovement);
        }

        public override void Update(float deltaTime) {
            if (_isPeasant) return;
            if (Movement.CurrentState == _rotateTowardsPlayer) {
                if (DistanceToHeroSqr() < _followAttemptSqr) {
                    StartFollowing();
                }
            } else if (Movement.CurrentState == _followMovement) {
                if (Npc.Coords.SquaredDistanceTo(Hero.Current.Coords) > _followAttemptSqr) {
                    StopInterrupting();
                }
            } else if (Movement.CurrentState == _returnToStart) {
                _delayToReactToHeroAgain -= deltaTime;
                if (_delayToReactToHeroAgain <= 0 && _npcCrimeReactions.IsSeeingHero) {
                    StartFollowing();
                }
            }
        }

        float DistanceToHeroSqr() => Npc.Coords.SquaredDistanceTo(Hero.Current.Coords);

        protected override void OnExit() {
            base.OnExit();
            Movement.StopInterrupting();
            Movement.ResetMainState(_rotateTowardsPlayer);
        }
    }
}