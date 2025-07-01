using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.Utility.Editor.UTK {
    public abstract class EditorWindowPresenter<T> : EditorWindow where T : EditorWindow {
        [SerializeField, CanBeNull] VisualTreeAsset prototype;

        // ReSharper disable once StaticMemberInGenericType - it will be used in derived classes
        protected static string WindowName { get; set; } = nameof(T);
        protected VisualElement _root;
        
        protected static T GetWindow() {
            T wnd = GetWindow<T>();
            wnd.titleContent = new GUIContent(WindowName);
            return wnd;
        }
        
        protected static T CreateWindow() {
            T wnd = CreateWindow<T>();
            wnd.titleContent = new GUIContent(WindowName);
            return wnd;
        }
        
        public virtual void CreateGUI() {
            SetupWindow();
        }

        protected abstract void CacheVisualElements(VisualElement windowRoot);
        
        void SetupWindow() {
            _root = rootVisualElement;
            
            if (prototype != null) {
                _root.Add(prototype.Instantiate());
            }
            
            CacheVisualElements(_root);
        }
    }
}
