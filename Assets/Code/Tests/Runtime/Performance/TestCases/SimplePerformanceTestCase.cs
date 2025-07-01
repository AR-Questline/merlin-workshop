using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.MovementSystems;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.Tests.Performance.TestCases {
    [Serializable]
    public class SimplePerformanceTestCase : IPerformanceTestCase {
        [field: SerializeField] public SceneReference Scene { get; private set; }
        [field: SerializeField] public string Name { get; private set; }

        [SerializeField, ListDrawerSettings(CustomAddFunction = nameof(NewWaypoint))] Waypoint[] waypoints;

        Hero _hero;
        VHeroController _heroController;
        Transform _heroTransform;

        State _state;
        int _destination;
        float _timeToWait;

        public void Run() {
            _hero = Hero.Current;
            _heroController = _hero.VHeroController;
            _heroTransform = _heroController.transform;
            
            SetupHeroForTest();
            _state = State.None;
            _destination = -1;
        }
        
        public void Update(out bool ended, out bool capture) {
            ended = false;
            capture = false;
            if (_destination >= waypoints.Length) {
                ended = true;
                ResetHeroAfterTest();
                return;
            }

            if (_destination >= 0) {
                capture = waypoints[_destination].capture;
            }

            if (_state == State.None) {
                _state = State.Moving;
                _destination++;
            } else if (_state == State.Moving) {
                ref readonly var destination = ref waypoints[_destination];
                if (destination.teleport) {
                    MoveTo(destination.position, destination.rotation);
                    _timeToWait = destination.waitTime;
                    _state = State.Waiting;
                } else {
                    var deltaTime = Time.deltaTime;
                    _heroTransform.GetPositionAndRotation(out var position, out var rotation);
                    
                    position = Vector3.MoveTowards(position, destination.position, destination.translationSpeed * deltaTime);
                    rotation = Quaternion.RotateTowards(rotation, destination.rotation, destination.rotationSpeed * deltaTime);
                    MoveTo(position, rotation);
                    
                    if (position == destination.position && rotation == destination.rotation) {
                        _timeToWait = destination.waitTime;
                        _state = State.Waiting;
                    }
                }
            } else if (_state == State.Waiting) {
                _timeToWait -= Time.deltaTime;
                if (_timeToWait <= 0) {
                    _state = State.None;
                }
            }
        }

        void SetupHeroForTest() {
            _heroController.Target.TrySetMovementType<NoClipMovement>();
            _heroController.gameObject.layer = LayerMask.NameToLayer("PlayerInteractions");
            _heroController.Controller.enabled = false;
        }

        void ResetHeroAfterTest() {
            _heroController.Target.ReturnToDefaultMovement();
            _heroController.gameObject.layer = LayerMask.NameToLayer("Player");
            _heroController.Controller.enabled = true;
        }

        void MoveTo(Vector3 position, Quaternion rotation) {
            _heroTransform.SetPositionAndRotation(position, rotation);
            _hero.MoveTo(position);
            _hero.Rotation = rotation;
        }

        Waypoint NewWaypoint() {
            if (waypoints.Length == 0) {
                return new Waypoint {
                    position = Vector3.zero,
                    rotation = Quaternion.identity,
                    teleport = true,
                    translationSpeed = 15,
                    rotationSpeed = 120,
                    waitTime = 10,
                };
            }
            if (waypoints.Length == 1) {
                return new Waypoint {
                    position = waypoints[0].position + Vector3.forward * 5,
                    rotation = Quaternion.LookRotation(Vector3.forward),
                    teleport = false,
                    translationSpeed = 15,
                    rotationSpeed = 120,
                    waitTime = 0,
                };
            }
            var dir = (waypoints[^1].position - waypoints[^2].position).normalized;
            return new Waypoint {
                position = waypoints[^1].position + dir * 5,
                rotation = Quaternion.LookRotation(dir.ToHorizontal3()),
                teleport = false,
                translationSpeed = 15,
                rotationSpeed = 120,
                waitTime = 0,
            };
        }

        [Serializable]
        public struct Waypoint {
            public Vector3 position;
            public Quaternion rotation;

            public bool capture;
            public bool teleport;
            public float translationSpeed;
            public float rotationSpeed;
            
            public float waitTime;
        }
        
        enum State : byte {
            None,
            Moving,
            Waiting
        }
        
        #if UNITY_EDITOR
        [SerializeField, LabelText("Editor Data")] EDITOR_Data _EDITOR_Data = EDITOR_Data.Default;
        
        [Serializable]
        public struct EDITOR_Data {
            public bool draw;
            public float size;
            public Color capturingPointColor;
            public Color defaultPointColor;
            public Color capturingConnectionColor;
            public Color defaultConnectionColor;
            public bool drawFrustum;
            public bool drawTransforms;

            public static EDITOR_Data Default => new() {
                draw = false,
                size = 1,
                capturingPointColor = Color.green,
                defaultPointColor = Color.red,
                capturingConnectionColor = Color.green * 0.8f,
                defaultConnectionColor = Color.red * 0.8f,
                drawFrustum = false,
            };
        }
        
        public struct EDITOR_Accessor {
            readonly SimplePerformanceTestCase _testCase;

            public ref Waypoint[] Waypoints => ref _testCase.waypoints;
            public ref EDITOR_Data EditorData => ref _testCase._EDITOR_Data;

            public EDITOR_Accessor(SimplePerformanceTestCase testCase) {
                _testCase = testCase;
            }
        }
        #endif
    }
}