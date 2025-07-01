using Awaken.TG.Code.Utility;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.Controllers.Rotation;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Grounds;
using Pathfinding;
using UnityEngine;

namespace Awaken.TG.Main.AI.Movement.States {
    public class Patrol : MovementState {
        CharacterPlace _centerPlace;
        CharacterPlace _randomPlace;
        VelocityScheme _velocityScheme;
        FloatRange? _waitTime;

        bool _haveSetDestination;
        
        bool _skipWait;
        float _timeToWander;

        public float Radius { get; private set; }
        public override VelocityScheme VelocityScheme => _velocityScheme;
        public Vector3 CurrentRandomPlace => _randomPlace.Position;

        public Patrol(CharacterPlace place, float radius, VelocityScheme velocityScheme, FloatRange? waitTime = null) {
            _centerPlace = place;
            _randomPlace = place;
            Radius = radius;
            _velocityScheme = velocityScheme;
            _waitTime = waitTime;
        }
        
        public void UpdatePlace(Vector3 place) {
            UpdatePlace(new CharacterPlace(place, _centerPlace.Radius));
        }

        public void UpdatePlace(CharacterPlace place) => UpdatePlace(place, place);
        
        public void UpdatePlace(CharacterPlace centerPlace, CharacterPlace randomPlace) {
            _centerPlace = centerPlace;
            _randomPlace = randomPlace;
            _haveSetDestination = false;
            if (IsSetUp && ActiveSelf) {
                _haveSetDestination = Controller.TrySetDestination(_randomPlace.Position);
            }
        }

        public void SelectNewRandomDestination() {
            _randomPlace = RandomDestination();
            _haveSetDestination = Controller.TrySetDestination(_randomPlace.Position);
        }

        [UnityEngine.Scripting.Preserve]
        public void UpdateRadius(float radius) {
            Radius = radius;
        }
        
        public void UpdateVelocityScheme(VelocityScheme velocityScheme) {
            _velocityScheme = velocityScheme;
        }

        protected override void OnPush() {
            _skipWait = true;
        }

        protected override void OnEnter() {
            Controller.onReached += OnReached;
            Controller.SetRotationScheme(new RotateTowardsMovement(), VelocityScheme);

            if (!_skipWait && _waitTime.HasValue) {
                float waitTime = _waitTime.Value.RandomPick();
                if (waitTime > 0) {
                    _timeToWander = waitTime;
                    return;
                }
            } 
            
            _skipWait = false;
            _timeToWander = -1;
            if (_randomPlace == _centerPlace) {
                SelectNewRandomDestination();
            }
        }

        protected override void OnExit() {
            Controller.onReached -= OnReached;
        }

        protected override void OnUpdate(float deltaTime) {
            if (_timeToWander >= 0) {
                _timeToWander -= deltaTime;
                if (_timeToWander < 0) {
                    SelectNewRandomDestination();
                }
            }

            if (!_haveSetDestination) {
                _haveSetDestination = Controller.TrySetDestination(_randomPlace.Position);
            }
        }

        void OnReached() {
            if (_waitTime.HasValue) {
                float waitTime = _waitTime.Value.RandomPick();
                if (waitTime > 0) {
                    _timeToWander = waitTime;
                    return;
                }
            }
            SelectNewRandomDestination();
        }
        
        public CharacterPlace RandomDestination() {
            Vector3 destination = Ground.SnapNpcToGround(_centerPlace.Position + new Vector3(RandomInRadius(), 0, RandomInRadius()));
            
            if (AstarPath.active) {
                NNInfo resultNode = AstarPath.active.GetNearest(destination);
                if (resultNode.node != null) {
                    destination = resultNode.position;
                }
            }
            
            return new CharacterPlace(destination, _centerPlace.Radius);
        }
        
        float RandomInRadius() {
            return RandomUtil.UniformFloat(-Radius, Radius);
        }
    }
}