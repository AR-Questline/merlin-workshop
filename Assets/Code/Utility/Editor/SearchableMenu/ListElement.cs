using Awaken.TG.Main.UIToolkit;
using UnityEditor;
using UnityEngine.UIElements;

namespace Awaken.Utility.Editor.SearchableMenu {
    public class ListElement : VisualElement {
        const string HighlightClass = "highlight";
        const string SelectClass = "select";

        VisualElement _root;
        Label _value;
        Label _path;
        VisualElement _arrow;

        public string Text {
            get => _value.text;
            set => _value.text = value;
        }

        public bool IsLeaf {
            get => !_arrow.visible;
            set => _arrow.SetActiveOptimized(!value);
        }

        public string Path {
            get => _path.text;
            set => _path.text = string.IsNullOrEmpty(value) ? value : $"({value})";
        }

        public ListElement() {
            LoadPrototype();
        }

        public void ToggleHighlight() => _root.ToggleInClassList(HighlightClass);
        public void ToggleSelect() => _root.ToggleInClassList(SelectClass);
        
        void LoadPrototype() {
            VisualTreeAsset prototype = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Code/Utility/Editor/SearchableMenu/ListElement.uxml");
            prototype.CloneTree(this);
            CacheElements();
        }

        void CacheElements() {
            _root = this.Q<VisualElement>("list-element");
            _value = this.Q<Label>("value");
            _arrow = this.Q<VisualElement>("arrow");
            _path = this.Q<Label>("path");
        }
    }
}