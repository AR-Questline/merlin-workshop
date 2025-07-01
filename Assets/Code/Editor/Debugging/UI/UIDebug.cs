using Awaken.TG.Editor.Debugging.UI.UIEventTypes;
using Awaken.TG.Editor.Utility.Tables;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Debugging.UI {
    public class UIDebug : OdinEditorWindow {
        const int FrameLimit = 1001;
        
        public static UIDebug Instance { get; private set; }

        bool _isRecording;
        
        UIEventsCollector _collector;
        FrameData _currentFrameData;
        
        EditorTable _framesTable;
        EditorTable _eventsTable;
        bool _framesTableIsActive;
        
        UIEventType _selectedEvent;
        int _selectedFrame;
        EditorTable _interactionTreeTable;

        EditorTable SelectedEventsInFrameTable => UIDebugLayout.CreateEventsInFrameTable(_collector.Frame(_selectedFrame));
        EditorTable SelectedInteractionTreeTable => UIDebugLayout.CreateInteractionTree(_collector.Frame(_selectedFrame).InteractionsOf(_selectedEvent));

        
        protected override void OnEnable() {
            base.OnEnable();
            _collector = new UIEventsCollector(FrameLimit);
        }

        protected override void OnImGUI() {
            GUILayout.Space(UIDebugLayout.TablesSpacing);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(UIDebugLayout.TablesSpacing);
            if (_isRecording) {
                if (GUILayout.Button("Stop", GUILayout.Width(UIDebugLayout.StartStopButtonWidth))) {
                    Stop();
                }
            } else {
                if (GUILayout.Button("Start", GUILayout.Width(UIDebugLayout.StartStopButtonWidth))) {
                    Start();
                }
            }
            if (GUILayout.Button("Clear", GUILayout.Width(UIDebugLayout.StartStopButtonWidth))) {
                Refresh(true);
            }
            EditorGUILayout.EndHorizontal();

            if (_isRecording) {
                EditorGUILayout.LabelField("Cannot browse data when recording");
            } else {
                DrawTables();
            }
        }

        void DrawTables() {
            float height = position.height - UIDebugLayout.TablesTop - UIDebugLayout.TablesSpacing;

            DrawTabButtons(UIDebugLayout.TablesSpacing, UIDebugLayout.TablesTop, UIDebugLayout.LookupWidth, UIDebugLayout.LookupTabHeight);

            if (_framesTableIsActive) {
                DrawFramesTable(UIDebugLayout.TablesSpacing, UIDebugLayout.TablesTop + UIDebugLayout.LookupTabHeight, UIDebugLayout.LookupWidth, height - UIDebugLayout.LookupTabHeight);
            } else {
                DrawEventsTable(UIDebugLayout.TablesSpacing, UIDebugLayout.TablesTop + UIDebugLayout.LookupTabHeight, UIDebugLayout.LookupWidth, height - UIDebugLayout.LookupTabHeight);
            }
            
            DrawInteractionTreeTable(2*UIDebugLayout.TablesSpacing+UIDebugLayout.LookupWidth, UIDebugLayout.TablesTop, position.width - 3*UIDebugLayout.TablesSpacing - UIDebugLayout.LookupWidth, height);
        }

        void DrawTabButtons(float left, float top, float width, float height) {
            float halfHeight = height / 2;
            
            float tabWidth = width / 2;
            if (GUI.Button(new Rect(left, top, tabWidth, halfHeight), UIDebugLayout.FrameTab)) {
                _framesTableIsActive = true;
            }
            if (GUI.Button(new Rect(left + tabWidth, top, tabWidth, halfHeight), UIDebugLayout.EventTab)) {
                _framesTableIsActive = false;
            }
            
            EditorGUI.LabelField(new Rect(left, top + halfHeight, width, halfHeight), _framesTableIsActive ? UIDebugLayout.FrameTab : UIDebugLayout.EventTab);
        }

        void DrawFramesTable(float left, float top, float width, float height) {
            float sliderHeight = EditorGUIUtility.singleLineHeight;
            DrawFrameSlider(left, top, width, sliderHeight);

            if (_framesTable == null) {
                EditorGUI.LabelField(new Rect(left, top + sliderHeight + UIDebugLayout.TablesSpacing, width, EditorGUIUtility.singleLineHeight), "Frame not recorded");
            } else {
                _framesTable.OnGUI(new Rect(left, top + sliderHeight + UIDebugLayout.TablesSpacing, width, height - sliderHeight));

                var selectedEvent = (_framesTable.FirstSelected() as EventInFrameItem)?.EventType;
                if (!_selectedEvent?.Equals(selectedEvent) ?? selectedEvent != null) {
                    _selectedEvent = selectedEvent;
                    _interactionTreeTable = _selectedEvent != null ? SelectedInteractionTreeTable : null;
                    Repaint();
                }
            }
        }

        void DrawFrameSlider(float left, float top, float width, float height) {
            var frame = EditorGUI.IntSlider(new Rect(left, top, width-2*UIDebugLayout.ArrowButtonWidth, height), _selectedFrame, 0, FrameLimit - 1);

            if (GUI.Button(new Rect(left + width - 2 * UIDebugLayout.ArrowButtonWidth, top, UIDebugLayout.ArrowButtonWidth, height), "<")) {
                frame--;
            }
            if (GUI.Button(new Rect(left + width - UIDebugLayout.ArrowButtonWidth, top, UIDebugLayout.ArrowButtonWidth, height), ">")) {
                frame++;
            }

            if (Event.current.type == EventType.KeyDown) {
                switch (Event.current.keyCode) {
                    case KeyCode.A:
                        frame--;
                        Event.current.Use();
                        break;
                    case KeyCode.D:
                        frame++;
                        Event.current.Use();
                        break;
                }
            }

            frame = Mathf.Clamp(frame, 0, FrameLimit);
            
            if (frame != _selectedFrame) {
                _selectedFrame = frame;
                _selectedEvent = null;
                _framesTable = SelectedEventsInFrameTable;
                _interactionTreeTable = null;
                Repaint();
            }
        }

        void DrawEventsTable(float left, float top, float width, float height) {
            if (_eventsTable == null) {
                EditorGUI.LabelField(new Rect(left, top + UIDebugLayout.TablesSpacing, width, EditorGUIUtility.singleLineHeight), "No events recorded");
            } else {
                _eventsTable.OnGUI(new Rect(left, top, width, height));
            }
        }

        void DrawInteractionTreeTable(float left, float top, float width, float height) {
            float labelHeight = EditorGUIUtility.singleLineHeight;
            EditorGUI.LabelField(new Rect(left, top, width, labelHeight), $"Event: {UIDebugLayout.NameOf(_selectedEvent)}  Frame: {_selectedFrame}");
            _interactionTreeTable?.OnGUI(new Rect(left, top + labelHeight, width, height - labelHeight));
        }

        public void SelectFrameAndEvent(int frame, UIEventType evt) {
            bool changed = !(_selectedEvent?.Equals(evt) ?? evt == null) || _selectedFrame != frame;
            _selectedEvent = evt;
            _selectedFrame = frame;
            if (changed) {
                _interactionTreeTable = SelectedInteractionTreeTable;
                _framesTable = SelectedEventsInFrameTable;
                Repaint();
            }
        }

        void Refresh(bool clear) {
            if (clear) {
                _collector.Clear();
            }
            _eventsTable = UIDebugLayout.CreateFramesByEventTable(_collector);
            _framesTable = null;
            _interactionTreeTable = null;

            _selectedFrame = -1;
            _selectedEvent = null;

            Instance = this;
            Repaint();
        }

        void Start() {
            GameUI gameUI = World.Any<GameUI>();
            if (gameUI == null) return;

            _isRecording = true;
            gameUI.Profiler.NewEvent += _collector.AddEvent;
            gameUI.Profiler.BeforeDelivery += _collector.AddResultOfHandlerBeforeDelivery;
            gameUI.Profiler.Handling += _collector.AddResultOfHandling;
            gameUI.Profiler.BeforeHandling += _collector.AddResultOfHandlerBeforeHandling;
            gameUI.Profiler.AfterHandling += _collector.AddResultOfHandlerAfterHandling;
        }

        void Stop() {
            GameUI gameUI = World.Any<GameUI>();
            if (gameUI == null) return;
            
            _isRecording = false;
            gameUI.Profiler.NewEvent -= _collector.AddEvent;
            gameUI.Profiler.BeforeDelivery -= _collector.AddResultOfHandlerBeforeDelivery;
            gameUI.Profiler.Handling -= _collector.AddResultOfHandling;
            gameUI.Profiler.BeforeHandling -= _collector.AddResultOfHandlerBeforeHandling;
            gameUI.Profiler.AfterHandling -= _collector.AddResultOfHandlerAfterHandling;

            Refresh(false);
        }

        [MenuItem ("TG/Debug/Debug UI")]
        static void ShowWindow() {
            var window = GetWindow<UIDebug>();
            window.titleContent = new GUIContent("Debug UI");
            window.Show ();
        }
    }
}