using System;
using System.Diagnostics;
using Awaken.TG.Main.Rendering;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility;
using Awaken.Utility.Maths;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Localization;

namespace Awaken.TG.Main.AI.Idle.Interactions.Patrols {
    [Serializable]
    public struct PatrolPath {
        [InfoBox("Select waypoint: RMB\nAdd waypoint: Shift + RMB\nRemove selected waypoint: Delete")]
        [SerializeField] public Type type;
        [SerializeField] public PatrolWaypoint[] waypoints;
        
        CapsuleCollider[] _capsules;

        public void Init(Transform parent) {
            for (int i = 0; i < waypoints.Length; i++) {
                Vector3 previous, current, next;
                if (type == Type.Cyclic) {
                    previous = waypoints[(i - 1 + waypoints.Length) % waypoints.Length].position;
                    current = waypoints[i].position;
                    next = waypoints[(i + 1) % waypoints.Length].position;
                } else {
                    if (i == 0) {
                        current = waypoints[i].position;
                        next = waypoints[i + 1].position;
                        previous = current - (next - current);
                    } else if (i == waypoints.Length - 1) {
                        previous = waypoints[i - 1].position;
                        current = waypoints[i].position;
                        next = current + (current - previous);
                    } else {
                        previous = waypoints[i - 1].position;
                        current = waypoints[i].position;
                        next = waypoints[i + 1].position;
                    }
                }
                waypoints[i].forward = ((current - previous).normalized + (next - current).normalized).normalized;
                waypoints[i].rotation = Quaternion.LookRotation(waypoints[i].forward);
            }

            for (int i = 0; i < waypoints.Length; i++) {
                waypoints[i].pathForward = (waypoints[(i+1)%waypoints.Length].position - waypoints[i].position).normalized;
            }

            _capsules = new CapsuleCollider[waypoints.Length - 1];
            for (int i = 0; i < _capsules.Length; i++) {
                var start = waypoints[i].position;
                var end = waypoints[i + 1].position;
                
                var center = (start + end) * 0.5f;
                var direction = end - start;

                var go = new GameObject();
                go.layer = RenderLayers.AIInteractions;
                
                var partTransform = go.transform;
                partTransform.SetParent(parent);
                partTransform.position = center;
                partTransform.forward = direction;

                var capsule = go.AddComponent<CapsuleCollider>();
                capsule.radius = 0.5f;
                capsule.height = direction.magnitude;
                capsule.direction = 2;

                _capsules[i] = capsule;
            }
        }

        public readonly ref readonly PatrolWaypoint GetWaypoint(in Index index) {
            return ref waypoints[index.index];
        }

        public readonly bool TryGetNextIndex(in Index index, out Index nextIndex) {
            if (type == Type.Cyclic) {
                nextIndex = new(false, (index.index + 1) % waypoints.Length);
                return true;
            } 
            if (type == Type.TwoWay) {
                if (index.backWay) {
                    if (index.index == 0) {
                        nextIndex = new(false, 1);
                        return true;
                    } else {
                        nextIndex = new(true, index.index - 1);
                        return true;
                    }
                } else {
                    if (index.index == waypoints.Length - 1) {
                        nextIndex = new(true, index.index - 1);
                        return true;
                    } else {
                        nextIndex = new(false, index.index + 1);
                        return true;
                    }
                }
            } 
            if (type == Type.OneWay) {
                if (index.index == waypoints.Length - 1) {
                    nextIndex = default;
                    return false;
                }
                nextIndex = new(false, index.index + 1);
                return true;
            }
            throw new ArgumentOutOfRangeException();
        }

        public readonly void RetrieveClosestWaypoint(Vector3 source, out Index index) {
            index = new(false, 0);
            float distanceSqr = (source - waypoints[index.index].position).sqrMagnitude;
            for (int i = 1; i < waypoints.Length; i++) {
                var newClosestPoint = waypoints[index.index].position;
                float newDistanceSq = (source - newClosestPoint).sqrMagnitude;
                if (newDistanceSq < distanceSqr) {
                    distanceSqr = newDistanceSq;
                    index = new (false, i);
                }
            }
        }
        
        public readonly void RetrieveClosestPathToPoint(Vector3 source, out Vector3 point, out Vector3 forward, out Index index) {
            index = new(false, 0);
            point = _capsules[0].ClosestPoint(source);
            float distanceSqr = (source - point).sqrMagnitude;
            for (int i = 1; i < _capsules.Length; i++) {
                var newClosesPoint = _capsules[i].ClosestPoint(source);
                float newDistanceSq = (source - newClosesPoint).sqrMagnitude;
                if (newDistanceSq < distanceSqr) {
                    distanceSqr = newDistanceSq;
                    point = newClosesPoint;
                    index = new (false, i);
                }
            }
            forward = _capsules[index.index].transform.forward;
        }

        public readonly Vector3 GetInterpolatedForward(Index reachedIndex, Vector3 point) {
            const float TurnDistance = 3f;
            const float TurnDistanceSq = TurnDistance * TurnDistance;

            var reachedWaypoint = waypoints[reachedIndex.index];
            float distanceToReachedSq = reachedWaypoint.position.SquaredDistanceTo(point);
            if (distanceToReachedSq < TurnDistanceSq) {
                return Vector3.Lerp(reachedWaypoint.forward, reachedWaypoint.pathForward, distanceToReachedSq / TurnDistanceSq);
            }

            if (TryGetNextIndex(reachedIndex, out var nextIndex)) {
                var nextWaypoint = waypoints[nextIndex.index];
                float distanceToNextSq = nextWaypoint.position.SquaredDistanceTo(point);
                if (distanceToNextSq < TurnDistanceSq) {
                    return Vector3.Lerp(reachedWaypoint.pathForward, nextWaypoint.forward, distanceToNextSq / TurnDistanceSq);
                }
            }

            return reachedWaypoint.pathForward;
        }
        
        public readonly struct Index {
            public readonly bool backWay;
            public readonly int index;

            public Index(bool backWay, int index) {
                this.backWay = backWay;
                this.index = index;
            }
        }
        
        public enum Type {
            Cyclic,
            OneWay,
            TwoWay,
        }
        
#if UNITY_EDITOR
        const string EditorGroup = "Editor";
        [SerializeField, FoldoutGroup(EditorGroup), LabelText("Snap To Ground")] bool EDITOR_snapToGround;
        [SerializeField, FoldoutGroup(EditorGroup), LabelText("Draw")] bool EDITOR_draw;
        [SerializeField, FoldoutGroup(EditorGroup), LabelText("Edge Color")] Color EDITOR_edgeColor;
        [SerializeField, FoldoutGroup(EditorGroup), LabelText("Selected Edge Color")] Color EDITOR_selectedEdgeColor;
        [SerializeField, FoldoutGroup(EditorGroup), LabelText("Vertex Color")] Color EDITOR_vertexColor;
        [SerializeField, FoldoutGroup(EditorGroup), LabelText("Vertex Radius")] float EDITOR_vertexRadius;
        
        [Space]
        [ShowInInspector, FoldoutGroup(EditorGroup), LabelText("Show All Handles")] static bool EDITOR_allHandles;

        static bool s_otherGizmosVisible;
        
        [FoldoutGroup(EditorGroup), Button]
        void ToggleNotSelectedVisible() {
            s_otherGizmosVisible = !s_otherGizmosVisible;
        }

        public bool EDITOR_SnapToGround => EDITOR_snapToGround;
        public bool EDITOR_AllHandles => EDITOR_allHandles;

        readonly Color EdgeColor(bool selected) => !selected 
                                                       ? EDITOR_edgeColor 
                                                       : EDITOR_selectedEdgeColor.a != 0 
                                                           ? EDITOR_selectedEdgeColor 
                                                           : Color.green;
        
        public readonly void EDITOR_DrawGizmos(bool selected = false) {
            if (!selected && !s_otherGizmosVisible) return;
            
            var defaultColor = Gizmos.color;
            if (EDITOR_draw) {
                for (int i = 0; i < waypoints.Length; i++) {
                    if (i == 0) {
                        if (type == Type.Cyclic) {
                            Gizmos.color = EdgeColor(selected);
                            Gizmos.DrawLine(waypoints[^1].position, waypoints[0].position);
                        }
                        Gizmos.color = EDITOR_vertexColor;
                        Gizmos.DrawCube(waypoints[0].position, 2 * EDITOR_vertexRadius * Vector3.one);
                    } else {
                        Gizmos.color = EdgeColor(selected);
                        Gizmos.DrawLine(waypoints[i-1].position, waypoints[i].position);
                        Gizmos.color = EDITOR_vertexColor;
                        Gizmos.DrawSphere(waypoints[i].position, EDITOR_vertexRadius);
                    }
                }
            }
            Gizmos.color = defaultColor;
        }

        public readonly void EDITOR_AlignTransformWithStartOfPath(Transform transform) {
            if (waypoints.Length == 0) {
                return;
            }
            transform.position = waypoints[0].position;
            if (waypoints.Length > 1) {
                var forward = waypoints[1].position - waypoints[0].position;
                forward.y = 0;
                transform.forward = forward;
            }
        }
#endif
        
        public static PatrolPath Default => new() {
            type = Type.OneWay,
            waypoints = new PatrolWaypoint[1],
#if UNITY_EDITOR
            EDITOR_snapToGround = true,
            EDITOR_draw = true,
            EDITOR_edgeColor = Color.white,
            EDITOR_vertexColor = Color.red,
            EDITOR_selectedEdgeColor = Color.green,
            EDITOR_vertexRadius = 0.1F,
#endif
        };
    }
    
    [Serializable]
    public struct PatrolWaypoint {
#if UNITY_EDITOR
        [NonSerialized] public bool EDITOR_selected;
        public static bool EDITOR_newSelection;
        [GUIColor("@this." + nameof(EDITOR_ButtonCollor))]
        [InlineButton(nameof(Editor_GoTo), "Go To")]
#endif
        public Vector3 position;
        public float lookAroundTime;
        public bool interactAround;
        
        [NonSerialized] public Vector3 forward;
        [NonSerialized] public Quaternion rotation;
        [NonSerialized] public Vector3 pathForward;
        
        [ShowIf(nameof(interactAround)), Tags(TagsCategory.Interaction)] public string interactionTag;
        [ShowIf(nameof(interactAround))] public float interactionRange;

        public PatrolWaypoint(Vector3 position) : this() {
            this.position = position;
        }

        PatrolWaypoint(PatrolWaypoint previousWaypoint) {
            this = previousWaypoint;
        }
        
        public PatrolWaypoint(Vector3 position, PatrolWaypoint previousWaypoint) : this(previousWaypoint) {
            this.position = position;
        }
        
        public PatrolWaypoint(float range, PatrolWaypoint previousWaypoint) : this(previousWaypoint) {
            this.interactionRange = range;
        }
        
        
#if UNITY_EDITOR
        void Editor_GoTo() {
            UnityEditor.SceneView.lastActiveSceneView.LookAt(position, Quaternion.Euler(90, 0, 0), 10);
            EDITOR_selected = true;
            EDITOR_newSelection = true;
        }
        
        Color EDITOR_ButtonCollor => EDITOR_selected ? Color.green : Color.white;
#endif
    }

    public interface IPatrolPathContainer {
        ref PatrolPath PatrolPath { get; }
    }
}