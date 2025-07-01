using System;
using Awaken.TG.Main.UIToolkit.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UIToolkit.MarvinUTK {
    public class DebugWindowUI {
        public VisualElement Root { get; private set; }
        public VisualElement Content { get; private set; }
        public UIDocument Document { get; }

        VisualElement _documentRoot;
        VisualElement _window;
        VisualElement _titleBar;

        Label _titleLabel;
        Button _closeButton;
        Button _minimizeButton;

        bool _minimized;

        readonly string _title;
        readonly Action _closeCallback;
        readonly AnchoredRect _initialPosition;
        SimpleDragBehaviour _dragBehaviour;

        public DebugWindowUI(UIDocument document, AnchoredRect initialPosition, string title, Func<VisualElement> drawContent, Action closeCallback, bool show = true) {
            Document = document;
            _title = title;
            _closeCallback = closeCallback;
            _initialPosition = initialPosition;

            Draw(drawContent);
            if (show) Show();
        }

        public void Show() {
            _minimized = false;
            _dragBehaviour.Enabled = true;
            Root.SetActiveOptimized(true);
        }

        public void Hide() {
            _dragBehaviour.Enabled = false;
            Root.SetActiveOptimized(false);
            Content.Clear();
        }

        void Draw(Func<VisualElement> drawContent) {
            _documentRoot = Document.rootVisualElement;
            Root = _documentRoot.Q<VisualElement>(_title);

            if (Root == null) {
                Root = Resources.Load<VisualTreeAsset>("DebugUI/DebugWindow").Instantiate();
                Root.name = _title;
                Root.AddToClassList("position-full-stretch");
                Root.pickingMode = PickingMode.Ignore;
                _documentRoot.Add(Root);
            }

            CacheVisualElements();
            _window.SetAnchoredRect(_initialPosition);
            
            Root.RegisterCallback<MouseDownEvent>(_ => {
                Root.BringToFront();
                _documentRoot.Query(className: "debug-window").ForEach(element => element.RemoveFromClassList("debug-window--activate"));
                _window.AddToClassList("debug-window--activated");
            });

            _titleLabel.text = _title;
            _closeButton.clickable = new Clickable(Close);
            _minimizeButton.clickable = new Clickable(MinimizedClicked);
            Content.Clear();
            Content.Add(drawContent());
            _dragBehaviour = new SimpleDragBehaviour(_window, _titleBar, true);
            RefreshMinimized();
        }

        void CacheVisualElements() {
            _window = Root.Q<VisualElement>("window");
            Content = _window.Q<VisualElement>("content");

            _titleBar = _window.Q<VisualElement>("title-bar");
            _titleLabel = _titleBar.Q<Label>("title");
            _closeButton = _titleBar.Q<Button>("close-button");
            _minimizeButton = _titleBar.Q<Button>("minimize-button");
        }

        void Close() {
            Hide();
            _closeCallback();
        }

        void MinimizedClicked() {
            _minimized = !_minimized;
            RefreshMinimized();
        }

        void RefreshMinimized() {
            Content.SetActiveOptimized(!_minimized);
            Root.EnableInClassList("position-full-stretch", !_minimized);
            _minimizeButton.SetTextColor(_minimized ? Color.yellow : Color.white);
        }
    }
}