using Awaken.TG.Graphics.DayNightSystem;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.TG.Editor.Graphics {
    [Overlay(typeof(SceneView), "Day Night System")]
    public class DayNightSystemOverlay : Overlay {
        const int TotalMinutesInDay = 1440;

        VisualElement _root;
        DayNightSystem.EDITOR_Access _access;
        Button _createSystemButton;
        Button _cycleButton;
        float _normalizedTimeOfDay = 0.5f;
        bool _isCycleEnabled;

        DayNightSystem DayNightSystem => _access.DayNightSystem;

        public override VisualElement CreatePanelContent() {
            if (_root != null) {
                _root.Clear();
            } else {
                _root = new VisualElement { name = "DayNightControlRoot" };
                _root.style.width = new StyleLength(400);
            }

            InitializeDayAndNightSystem();
            if (Application.isPlaying) {
                var infoLabel = new Label("Use '+' / '-' to change time of day");
                _root.Add(infoLabel);
            } else {
                if (_access.IsValid) {
                    CreateDayTimeSlider(_root);
                    CreateRotationSlider(_root);
                    CreateAdditionalOptions(_root);
                } else {
                    CreateSystem(_root);
                }
            }

            return _root;
        }
        void CreateDayTimeSlider(VisualElement root) {
            var horizontalLayout = new VisualElement();
            horizontalLayout.style.flexDirection = FlexDirection.Row;
    
            var timeOfDayLabel = new Label();
            timeOfDayLabel.style.width = 50; 
            UpdateTimeOfDayLabel(timeOfDayLabel);
            horizontalLayout.Add(timeOfDayLabel);

            var slider = new Slider(0f, 1f);
            slider.style.flexGrow = 1; 
            slider.value = _normalizedTimeOfDay;
            slider.RegisterValueChangedCallback(evt => {
                _normalizedTimeOfDay = evt.newValue;
                UpdateTimeOfDayLabel(timeOfDayLabel);
                _access.TimeOfDayChanged(_normalizedTimeOfDay);
            });
            horizontalLayout.Add(slider);

            root.Add(horizontalLayout);
        }
        
        void CreateRotationSlider(VisualElement root) {
            var horizontalLayout = new VisualElement();
            horizontalLayout.style.flexDirection = FlexDirection.Row;
    
            var rotationLabel = new Label();
            rotationLabel.style.width = 50; 
            UpdateRotationLabel(rotationLabel);
            rotationLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            horizontalLayout.Add(rotationLabel);

            var rotationSlider = new Slider(0f, 360f);
            rotationSlider.style.flexGrow = 1; 
            rotationSlider.value = DayNightSystem.HorizontalAngle;
            rotationSlider.RegisterValueChangedCallback(evt => {
                _access.EditorHorizontalAngle = evt.newValue;
                UpdateRotationLabel(rotationLabel);
            });
            horizontalLayout.Add(rotationSlider);

            root.Add(horizontalLayout);
        }

        void CreateAdditionalOptions(VisualElement root) {
            var horizontalLayout = new VisualElement();
            horizontalLayout.style.flexDirection = FlexDirection.Row;

            _cycleButton = new Button(ToggleCycle) { text = "Start Cycle" };
            _cycleButton.style.flexGrow = 1;
            horizontalLayout.Add(_cycleButton);

            var cycleTimeField = new FloatField("Day duration (min)");
            cycleTimeField.style.width = 150; 
            if (_access.IsValid) {
                cycleTimeField.value = _access.CycleValue;
                cycleTimeField.RegisterValueChangedCallback(evt => {
                    float newValue = Mathf.Clamp(evt.newValue, 0.1f, 60f);
                    _access.CycleValue = newValue;
                    cycleTimeField.value = newValue;
                });
            } else {
                cycleTimeField.value = 0f;
            }
            horizontalLayout.Add(cycleTimeField);
            
            root.Add(horizontalLayout);
            
            // Refresh button
            var refreshButton = new Button(RefreshOverlay) { text = "Refresh" };
            refreshButton.style.flexGrow = 1; 
            horizontalLayout.Add(refreshButton);
        }

        void CreateSystem(VisualElement root) {
            if (_createSystemButton == null) {
                _createSystemButton = new Button { text = "Create Day Night System" };
                _createSystemButton.clickable.clicked += CreateDayNightSystem;
            }

            root.Add(_createSystemButton);
        }

        void InitializeDayAndNightSystem() {
            _access.Initialize(Object.FindAnyObjectByType<DayNightSystem>());
            if (_access.IsValid) {
                _normalizedTimeOfDay = _access.EditorTimeOfDay;
            }
        }

        void UpdateTimeOfDayLabel(Label label) {
            float minutes = _normalizedTimeOfDay * TotalMinutesInDay;

            var hour = (int)(minutes / 60);
            var minute = (int)(minutes % 60);

            if (hour >= 24) {
                hour = 23;
                minute = 59;
                _normalizedTimeOfDay = 1.0f;
            }

            label.text = $"{hour:00}:{minute:00}";
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
        }
        
        void UpdateRotationLabel(Label label) {
            float angle = DayNightSystem.HorizontalAngle;
            label.text = $"{angle}°";
        }

        void ToggleCycle() {
            if (!_access.IsValid) {
                return;
            }
            _isCycleEnabled = !_isCycleEnabled;
            DayNightSystem.EDITOR_ToggleCycle();

            _cycleButton.text = _isCycleEnabled ? "Stop Cycle" : "Start Cycle";
        }

        void RefreshOverlay() {
            CreatePanelContent();
        }

        void CreateDayNightSystem() {
            // TODO: set final path
            const string PrefabPath = "Assets/3DAssets/Lighting/DayNightSystem/DayNightSystem.prefab";
            GameObject dayNightSystemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            if (dayNightSystemPrefab == null) {
                Debug.LogError("Prefab 'DayNightSystemPrefab' not found at path: " + PrefabPath);
                return;
            }

            PrefabUtility.InstantiatePrefab(dayNightSystemPrefab);
            CreatePanelContent();
        }
    }
}
