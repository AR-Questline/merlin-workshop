using UnityEngine;

namespace Awaken.Utility.UI {
    public abstract class UGUIWindowDisplay : MonoBehaviour { }
    public abstract class UGUIWindowDisplay<T> : UGUIWindowDisplay where T : UGUIWindowDisplay<T> {
        UGUIWindow _window;
        Vector2 _scroll;
        string _fullSearchContext = string.Empty;
        bool _minimized;

        protected virtual string Title => typeof(T).Name;
        protected virtual bool WithSearch => true;
        protected virtual bool WithScroll => true;
        protected virtual bool BlackBackground => true;
        protected SearchPattern SearchContext { get; private set; } = SearchPattern.Empty;
        protected Rect Position => _window.Position;
        protected Vector2 Scroll => _scroll;

        void Init(Rect position) {
            _window = new UGUIWindow(position, Title, DoDrawWindow, CloseInstance, DrawToolbarLeft, DrawToolbarRight);
            Initialize();
        }

        void DoDrawWindow() {
            if (_minimized) {
                return;
            }
            if (BlackBackground) {
                GUILayout.BeginVertical("box");
            }
            if (WithScroll) {
                _scroll = GUILayout.BeginScrollView(_scroll);
                DrawWindow();
                GUILayout.EndScrollView();
            } else {
                DrawWindow();
            }
            if (BlackBackground) {
                GUILayout.EndVertical();
            }
        }

        void CloseInstance() {
            Shutdown();
            s_instance = null;
            Destroy(gameObject);
        }

        void DrawToolbarLeft() {
            if (WithSearch) {
                GUILayout.Label("Search:");
                var change = new TGGUILayout.CheckChangeScope();
                _fullSearchContext = GUILayout.TextField(_fullSearchContext, GUILayout.ExpandWidth(true), GUILayout.MinWidth(200));
                if (change) {
                    SearchContext = new SearchPattern(_fullSearchContext);
                }
                change.Dispose();
            }
        }

        void DrawToolbarRight() {
            using (var colorScope = new ColorGUIScope(_minimized ? Color.yellow : Color.white)) {
                if (GUILayout.Button("-", GUILayout.Width(32))) {
                    _minimized = !_minimized;
                }
            }
        }

        void OnGUI() {
            _window.OnGUI();
        }

        protected virtual void Initialize(){}
        protected virtual void Shutdown(){}
        protected abstract void DrawWindow();

        // === Singleton
        static T s_instance;
        public static bool IsShown => s_instance != null;

        public static void Show(Rect position) {
            if (s_instance == null) {
                s_instance = new GameObject($"Window {typeof(T)}", typeof(T)).GetComponent<T>();
                s_instance.Init(position);
            }
        }

        public static void Close() {
            if (s_instance) {
                s_instance.CloseInstance();
            }
        }

        public static void Toggle(UGUIWindowUtils.WindowPositioning standardPosition) {
            Toggle(UGUIWindowUtils.StandardWindowPosition(standardPosition));
        }

        public static void Toggle(Rect position) {
            if (IsShown) {
                Close();
            } else {
                Show(position);
            }
        }
    }
}
