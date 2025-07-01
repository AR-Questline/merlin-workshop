using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using UnityEngine;

namespace Awaken.TG.Debugging.ModelsDebugs.Runtime {
    [UnityEngine.Scripting.Preserve]
    public class RuntimeModelsDebugNavigator : IModelsDebugNavigator {
        RuntimeTreeView _treeView;
        int _reloadTimer = 5;

        public string SearchString {
            get => _treeView.SearchString;
            set => _treeView.SearchString = value;
        }
        
        public void Draw(Rect rect) {
            _treeView.Draw(rect);
        }

        public MembersList GetSelected() {
            return _treeView.SelectedMember;
        }

        public void RefreshNavigation() {
            if (World.Services.Get<EventSystem>() == null) {
                _treeView = null;
                return;
            }

            if (_treeView == null) {
                _treeView = new RuntimeTreeView();
                var listener = new ModelsDebug.DebugListenerOwner();
                World.Add(listener);
                var eventSystem = World.Services.Get<EventSystem>();
                eventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelFullyInitializedAnyType, listener, (_) => _reloadTimer = 3);
                eventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscardedAnyType, listener, (_) => _reloadTimer = 3);
            }

            if (_treeView != null) {
                if (_reloadTimer > -1) {
                    --_reloadTimer;
                }
                if (_reloadTimer == 0) {
                    _treeView.Reload();
                }
            }
        }

        public bool TrySelectModel(string modelID) {
            return _treeView.TrySelect(modelID);
        }

        public void Frame() {
            
        }
    }
}