using System.Collections.Generic;
using System.Linq;
using Awaken.Utility.Previews;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Awaken.Utility.Editor.Previews {
    [CustomPreview(typeof(GameObject))]
    public class ARGameObjectPreview : ObjectPreview {
        Dictionary<Object, ARObjectPreview> _previewCache = new Dictionary<Object, ARObjectPreview>();

        public override void Initialize(Object[] targets) {
            base.Initialize(targets);

            DisposePreview();

            foreach (var singleTarget in targets) {
                if (singleTarget is not GameObject gameObject) {
                    continue;
                }
                var providers = gameObject.GetComponentsInChildren<IARPreviewProvider>();
                var preview = new ARObjectPreview(providers);
                if (preview.IsValid) {
                    _previewCache.Add(singleTarget, preview);
                } else {
                    preview.Dispose();
                }
            }
        }

        public override bool HasPreviewGUI() => _previewCache.Count > 0;

        public override void OnPreviewGUI(Rect r, GUIStyle background) {
            if (_previewCache.TryGetValue(target, out var preview)) {
                preview.OnPreviewGUI(r, background);
            }
        }

        public override GUIContent GetPreviewTitle() {
            var previewTitle = new GUIContent();
            var previewCount = _previewCache.Count;
            if (previewCount == 1) {
                previewTitle.text = $"AR preview {target.name}";
            } else if (previewCount > 1) {
                previewTitle.text = $"AR preview {previewCount} objects";
            } else {
                previewTitle.text = $"AR preview Empty";
            }
            return previewTitle;
        }

        public override void OnPreviewSettings() {
            base.OnPreviewSettings();
            if (GUILayout.Button("★", GUILayout.Width(25))) {
                SettingsService.OpenUserPreferences("Preferences/TG");
            }
        }

        public override void Cleanup() {
            DisposePreview();
            base.Cleanup();
        }

        void DisposePreview() {
            foreach (var preview in _previewCache.Values) {
                preview.Dispose();
            }
            _previewCache.Clear();
        }
    }

    class ARObjectPreview {
        static readonly GUIContent[] LightIcons = { null, null };

        List<IARRendererPreview> _rendererPreviews = new List<IARRendererPreview>();

        PreviewRenderUtility _previewUtility;
        Vector2 _previewDir = new Vector2(0, -20);
        float _distanceModifier = 15f;

        public ARObjectPreview(IARPreviewProvider[] previewProviders) {
            foreach (var previewProvider in previewProviders) {
                _rendererPreviews.AddRange(previewProvider.GetPreviews());
            }
        }

        public bool IsValid => _rendererPreviews.Any(static p => p.IsValid);

        public void Dispose() {
            foreach (var renderersPreview in _rendererPreviews) {
                renderersPreview.Dispose();
            }
            _rendererPreviews.Clear();
            _previewUtility?.Cleanup();
        }

        public void OnPreviewGUI(Rect r, GUIStyle background) {
            InitPreview();
            _previewDir = Drag2D(_previewDir, r);
            _previewDir.y = Mathf.Clamp(_previewDir.y, -140, 140);

            if (Event.current.type != EventType.Repaint) {
                return;
            }

            _previewUtility.BeginPreview(r, background);

            DoRenderPreview();

            _previewUtility.EndAndDrawPreview(r);
        }

        void InitPreview() {
            _previewUtility ??= new PreviewRenderUtility();

            if (LightIcons[0] == null) {
                LightIcons[0] = EditorGUIUtility.IconContent("PreMatLight0");
                LightIcons[1] = EditorGUIUtility.IconContent("PreMatLight1");
            }
        }

        void DoRenderPreview() {
            Bounds bounds = default;
            bool initBounds = true;

            for (int i = 0; i < _rendererPreviews.Count; i++) {
                var rendererPreview = _rendererPreviews[i];
                if (rendererPreview.IsValid == false) {
                    continue;
                }
                if (initBounds) {
                    initBounds = false;
                    bounds = rendererPreview.WorldBounds;
                } else {
                    bounds.Encapsulate(rendererPreview.WorldBounds);
                }
            }

            var halfSize = bounds.extents.magnitude;
            var distance = halfSize * _distanceModifier;

            var viewDir = -(_previewDir / 100f);

            _previewUtility.camera.transform.position = bounds.center +
                                                        (new Vector3(Mathf.Sin(viewDir.x) * Mathf.Cos(viewDir.y),
                                                             Mathf.Sin(viewDir.y),
                                                             Mathf.Cos(viewDir.x) * Mathf.Cos(viewDir.y))
                                                         * distance);

            _previewUtility.camera.transform.LookAt(bounds.center);
            _previewUtility.camera.nearClipPlane = 0.05f;
            _previewUtility.camera.farClipPlane = 10_000.0f;

            var lights = _previewUtility.lights;
            lights[0].intensity = EditorPrefs.GetFloat("lightIntensityMainDrakePreview", 1f);
            lights[0].color = ReadColor("lightColorMainDrakePreview", new Color(0.769f, 0.769f, 0.769f, 1f));
            lights[0].transform.rotation = Quaternion.Euler(ReadVector3("lightDirectionMainDrakePreview", new Vector3(50, 50, 0)));

            lights[1].intensity = EditorPrefs.GetFloat("lightIntensitySecondaryDrakePreview", 1f);
            lights[1].color = ReadColor("lightColorSecondaryDrakePreview", new Color(0.28f, 0.28f, 0.315f, 0.0f));
            lights[1].transform.rotation = Quaternion.Euler(ReadVector3("lightDirectionSecondaryDrakePreview", new Vector3(340f, 218f, 177f)));

            _previewUtility.ambientColor = ReadColor("ambientColorDrakePreview", new Color(.2f, .2f, .2f, 0));

            _previewUtility.camera.backgroundColor = ReadColor("backgroundColorDrakePreview", new Color(0.85f, 0.85f, 0.85f, 1f));
            if (QualitySettings.activeColorSpace == ColorSpace.Linear) {
                _previewUtility.camera.backgroundColor = _previewUtility.camera.backgroundColor.linear;
            }
            _previewUtility.camera.clearFlags = CameraClearFlags.SolidColor;

            for (int i = 0; i < _rendererPreviews.Count; i++) {
                var rendererPreview = _rendererPreviews[i];
                if (rendererPreview.IsValid == false) {
                    continue;
                }
                var mesh = rendererPreview.Mesh;
                var materials = rendererPreview.Materials;
                var renderingCount = math.min(mesh.subMeshCount, materials.Length);
                for (int j = 0; j < renderingCount; j++) {
                    _previewUtility.DrawMesh(mesh, rendererPreview.Matrix, materials[j], j);
                }
            }

            _previewUtility.Render(Unsupported.useScriptableRenderPipeline);
        }

        // Copied from UnityEditor.PreviewGUI
        static readonly int SliderHash = "Slider".GetHashCode();
        Vector2 Drag2D(Vector2 scrollPosition, Rect position) {
            int controlId = GUIUtility.GetControlID(SliderHash, FocusType.Passive);
            Event current = Event.current;
            switch (current.GetTypeForControl(controlId)) {
                case EventType.MouseDown:
                    if (position.Contains(current.mousePosition) && position.width > 50.0) {
                        GUIUtility.hotControl = controlId;
                        current.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId)
                        GUIUtility.hotControl = 0;
                    EditorGUIUtility.SetWantsMouseJumping(0);
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlId) {
                        scrollPosition -= current.delta *
                                          (current.shift ? 3f : 1f) /
                                          Mathf.Min(position.width, position.height) *
                                          140f;
                        current.Use();
                        GUI.changed = true;
                    }
                    break;

                case EventType.ScrollWheel:
                    if (position.Contains(current.mousePosition) && position.width > 50.0) {
                        var speed = 0.1f;
                        if (current.shift) {
                            speed *= 2f;
                        } else if (current.control) {
                            speed *= 0.3f;
                        }
                        _distanceModifier += current.delta.y * speed;
                        current.Use();
                        GUI.changed = true;
                    }
                    break;

            }
            return scrollPosition;
        }

        static Vector3 ReadVector3(string baseKey, Vector3 defaultValue) {
            var x = EditorPrefs.GetFloat($"{baseKey}.x", defaultValue.x);
            var y = EditorPrefs.GetFloat($"{baseKey}.y", defaultValue.y);
            var z = EditorPrefs.GetFloat($"{baseKey}.z", defaultValue.z);
            return new Vector3(x, y, z);
        }

        static Color ReadColor(string baseKey, Color defaultValue) {
            var r = EditorPrefs.GetFloat($"{baseKey}.r", defaultValue.r);
            var g = EditorPrefs.GetFloat($"{baseKey}.g", defaultValue.g);
            var b = EditorPrefs.GetFloat($"{baseKey}.b", defaultValue.b);
            var a = EditorPrefs.GetFloat($"{baseKey}.a", defaultValue.a);
            return new Color(r, g, b, a);
        }
    }
}