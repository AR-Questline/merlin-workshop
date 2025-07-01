using Awaken.TG.Main.Heroes;
using Pathfinding;
using UnityEngine;

namespace Awaken.TG.Main.Fights.Mounts {
    public class MountHeroSeeker {
        int _currentDestination;
        Vector3[] _destinations;

        VMount _mount;
        Seeker _seeker;

        Transform _heroTransform;
        Transform _mountTransform;

        public bool IsSeekingHero { get; private set; }
        public float DistanceToHero => (_mount.Target.ParentModel.Coords - Hero.Current.Coords).magnitude;
        
        public MountHeroSeeker(VMount mount) {
            _mount = mount;
            _seeker = mount.gameObject.AddComponent<Seeker>();
            
            _mountTransform = mount.transform;
            _heroTransform = Hero.Current.ParentTransform;
        }

        public Vector2 GetDesiredMovement(float deltaTime) {
            if (!IsSeekingHero) return Vector2.zero;

            SeekHero();

            Vector3 currentPosition = _mountTransform.position;
            Vector3 targetPosition = GetCurrentDestination() ?? currentPosition + _mountTransform.forward;

            Vector3 targetDirection = Vector3.ProjectOnPlane(targetPosition - currentPosition, Vector3.up).normalized;
            Vector3 currentDirection = Vector3.ProjectOnPlane(_mountTransform.forward, Vector3.up).normalized;

            float forwardMovement = Vector3.Dot(targetDirection, currentDirection);

            float sideMovement = Vector3.SignedAngle(currentDirection, targetDirection, Vector3.up) / 90.0f;
            float sideMovementClamped = Mathf.Clamp(sideMovement, -1, 1);

            return new Vector2(sideMovementClamped, forwardMovement);
        }
        
        public void TeleportCloserAndStartSeeking() {
            IsSeekingHero = false;

            if (ShouldTeleportHorse()) {
                TeleportHorseCloserAccessible();
            } else {
                StartSeeking();
            }
        }
        
        void StartSeeking() {
            if (IsSeekingHero) return;

            IsSeekingHero = true;
            _seeker.StartPath(_mount.Target.ParentModel.Coords,
                Hero.Current.Coords + _heroTransform.forward, OnPathGenerated);
        }

        public void EndSeeking() {
            IsSeekingHero = false;
        }

        void OnPathGenerated(Path path) {
            _destinations = path?.vectorPath?.ToArray();
            if (_destinations != null) {
                _currentDestination = 0;
                IsSeekingHero = true;
            } else {
                EndSeeking();
            }
        }

        void SeekHero() {
            Vector3? targetPosition = GetCurrentDestination();

            if (targetPosition == null) {
                EndSeeking();

                if (!IsHorseCloseToHero()) {
                    StartSeeking();
                }

                return;
            }

            float distanceToSeekedPoint = (_mountTransform.position - targetPosition.Value).magnitude;
            if (distanceToSeekedPoint < _mount.Data.requiredDistanceToSeekedPoint) {
                UseNextPath();
            }

            if (IsHorseCloseToHero()) {
                IsSeekingHero = false;
            }
        }


        Vector3? GetCurrentDestination() {
            if (_destinations != null && _currentDestination >= 0 && _currentDestination < _destinations.Length) {
                return _destinations[_currentDestination];
            }
            
            return null;
        }

        bool IsHorseCloseToHero() {
            return DistanceToHero < _mount.Data.requiredDistanceToSeekedHero;
        }
        
        bool ShouldTeleportHorse() {
            return DistanceToHero > _mount.Data.minDistanceForTeleportation;
        }

        void UseNextPath() {
            _currentDestination++;
        }

        void TeleportHorseCloserAccessible() {
            _seeker.StartPath(Hero.Current.Coords, FindRandomPointBehindHero(),
                OnAccessibleTeleportPointFound);
        }

        void TeleportHorseCloserSimple() {
            _mount.Teleport(FindRandomPointBehindHero() + Vector3.up * 2.0f);
        }

        Vector3 FindRandomPointBehindHero() {
            var randomPoint = Hero.Current.Coords;
            randomPoint -= _heroTransform.forward * Random.Range(5, 15);
            randomPoint += _heroTransform.right * Random.Range(-10, 10);

            return randomPoint;
        }

        void OnAccessibleTeleportPointFound(Path path) {
            Vector3[] pathPoints = path?.vectorPath?.ToArray();
            if (pathPoints is { Length: > 0 }) {
                _mount.Teleport(pathPoints[^1] + Vector3.up * 2.0f);
            } else {
                TeleportHorseCloserSimple();
            }

            StartSeeking();
        }
    }
}
