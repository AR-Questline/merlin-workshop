using System.Collections.Generic;
using Awaken.TG.Main.AI.States;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.AI.Debugging {
    public class PerceptionDebug {
        const float GridOffset = 0.5f;
        const float HeightOffset = 0.5f;
        const int UpdateTicks = 10;
        
        static List<GameObject> s_unusedVisionMarkers;
        
        int _updateTicks;
        
        // Vision
        Transform _visionMarkersParent;
        List<DebugPoint> _points;
        List<GameObject> _markers;
        float _angleRangeModifier;
        float _sightValue;
        float _sightLengthValue;
        float _maxDist;
        // Hearing
        Transform _hearingCircleMarker;
        // Alert
        Transform _alertPositionMarker;
        
        Perception _perception;
        PerceptionData _data;
        NpcAI _ai;

        public PerceptionDebug(NpcAI ai, Perception perception, PerceptionData data) {
            s_unusedVisionMarkers ??= new();
            _ai = ai;
            _perception = perception;
            _data = data;
            
            Init();
        }

        void Init() {
            var markersParent = _ai.ParentModel.Hips.parent;
            if (Perception.debugVisionMode) {
                CalculatePoints();
                CreateVisionMarkers(markersParent);
            }
            if (Perception.debugNoiseMode) {
                CreateHearingCircleMarker(markersParent);
            }
            if (Perception.debugTargetMode) {
                CreateAlertMarker(markersParent);
                UpdateAlertMarkerState();
            }
        }

        void CalculatePoints() {
            _points = new();
            _sightValue = _ai.NpcElement.NpcStats.Sight.ModifiedValue;
            _sightLengthValue =_ai.NpcElement.NpcStats.SightLengthMultiplier.ModifiedValue;
            _maxDist = _data.MaxDistance(_ai);
            var forward = _perception.GetLookForward(out var visionRangeMultiplier).ToHorizontal2();
            float lengthMultiplier = _sightLengthValue * visionRangeMultiplier;
            float maxDist = _maxDist * lengthMultiplier;
            for (float x = -maxDist; x <= maxDist; x += GridOffset) {
                for (float y = -maxDist; y <= maxDist; y += GridOffset) {
                    DebugPoint point = new();
                    point.localPos = new Vector2(x, y);
                    Vector3 direction = point.localPos.ToHorizontal3();
                    var dotProduct = Vector2.Dot(direction.ToHorizontal2().normalized, forward);
                    float distance = direction.magnitude;
                    point.vision = _perception.CalculateVisibilityFactor(dotProduct, distance, lengthMultiplier) * _sightValue;
                    if (point.vision > 0f && distance < maxDist) {
                        _points.Add(point);
                    }
                }
            }
        }

        void CreateVisionMarkers(Transform parent) {
            const float MarkerHeight = 0.3f;
            const float MarkerWidth = 0.5f;
            
            _markers = new();
            GameObject markersParent = new("Vision Debug");
            _visionMarkersParent = markersParent.transform;
            _visionMarkersParent.localPosition = parent.position + Vector3.up * HeightOffset;
            _visionMarkersParent.localRotation = Quaternion.LookRotation(_perception.GetLookForward(out _angleRangeModifier), Vector3.up);
            Vector3 scale = new Vector3(MarkerWidth, MarkerHeight, MarkerWidth);
            foreach (var point in _points) {
                var markerGo = GetVisionMarkerCube();
                markerGo.transform.SetParent(_visionMarkersParent);
                markerGo.transform.SetPositionAndRotation(_visionMarkersParent.position + point.localPos.ToHorizontal3(), Quaternion.identity);
                markerGo.transform.localScale = scale;
                Material material = markerGo.GetComponent<MeshRenderer>().material;
                material.color = GetColor(point.vision.Remap(_data.VisionCutoff, 1.5f, 0f, 1f));
                _markers.Add(markerGo);
            }
            _visionMarkersParent.SetParent(_ai.ParentModel.Hips.parent); // because AIs are rescaled it needs to be done at the end
        }

        GameObject GetVisionMarkerCube() {
            GameObject marker;
            if (s_unusedVisionMarkers.Count == 0) {
                marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = "Vision Debug Cube";
                Object.Destroy(marker.GetComponent<Collider>());
            } else {
                marker = s_unusedVisionMarkers[0];
                s_unusedVisionMarkers.RemoveAt(0);
                if (marker == null) {
                    return GetVisionMarkerCube();
                }
                marker.SetActive(true);
            }
            return marker;
        }
        
        // HEARING
        
        void CreateHearingCircleMarker(Transform parent) {
            const float MarkerHeight = 0.1f;
            
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Hearing Debug";
            Object.Destroy(go.GetComponent<Collider>());
            _hearingCircleMarker = go.transform;
            float markerWidth = _data.MaxHearingRange * _ai.NpcElement.NpcStats.Hearing.ModifiedValue * 2;
            _hearingCircleMarker.localScale = new Vector3(markerWidth, MarkerHeight, markerWidth);
            _hearingCircleMarker.localPosition = parent.position + Vector3.up * HeightOffset;
            _hearingCircleMarker.SetParent(parent); // because AIs are rescaled it needs to be done at the end
        }
        
        // ALERT POSITION

        void CreateAlertMarker(Transform parent) {
            const float MarkerHeight = 0.75f;
            const float MarkerWidth = 2f;
            
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Alert Debug";
            Object.Destroy(go.GetComponent<Collider>());
            _alertPositionMarker = go.transform;
            _alertPositionMarker.localScale = new Vector3(MarkerWidth, MarkerHeight, MarkerWidth);
            _alertPositionMarker.SetParent(parent);
        }
        
        void UpdateAlertMarkerState() {
            var position = _ai.AlertTarget + Vector3.up * HeightOffset;
            _alertPositionMarker.position = position;
            Material material = _alertPositionMarker.GetComponent<MeshRenderer>().material;
            material.color = GetColor(1 - _ai.AlertValue / 100f);
        }
        
        public void Regenerate() {
            Clear();
            Init();
        }

        public void Clear() {
            if (_points != null) {
                _points.Clear();
                s_unusedVisionMarkers.AddRange(_markers);
                foreach (var marker in _markers) {
                    marker.SetActive(false);
                    marker.transform.SetParent(null);
                }
                _markers.Clear();
                if (_visionMarkersParent != null) {
                    Object.Destroy(_visionMarkersParent.gameObject);
                    _visionMarkersParent = null;
                }
            }

            if (_hearingCircleMarker != null) {
                Object.Destroy(_hearingCircleMarker.gameObject);
                _hearingCircleMarker = null;
            }

            if (_alertPositionMarker != null) {
                Object.Destroy(_alertPositionMarker.gameObject);
                _alertPositionMarker = null;
            }
        }

        public void Update() {
            if (_updateTicks > 0) {
                _updateTicks--;
            }
            _updateTicks = UpdateTicks;

            if (Perception.debugVisionMode) {
                _visionMarkersParent.rotation = Quaternion.LookRotation(_perception.GetLookForward(out var angleRangeModifier), Vector3.up);
                if (Mathf.Abs(angleRangeModifier - _angleRangeModifier) > 0.1f) {
                    Regenerate();
                    return;
                }

                if (_sightValue != _ai.NpcElement.NpcStats.Sight.ModifiedValue || _sightLengthValue != _ai.NpcElement.NpcStats.SightLengthMultiplier.ModifiedValue) {
                    Regenerate();
                    return;
                }

                if (_maxDist != _data.MaxDistance(_ai)) {
                    Regenerate();
                    return;
                }
            }

            if (Perception.debugTargetMode) {
                UpdateAlertMarkerState();
            }
        }

        public static Color GetColor(float lerpValue) {
            return new(Mathf.Clamp01(2f * (1 - lerpValue)), Mathf.Clamp01(2f * lerpValue), 0f);
        }

        struct DebugPoint {
            public Vector2 localPos;
            public float vision;
        }
    }
}