using Awaken.TG.Debugging.ModelsDebugs;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Awaken.TG.Editor.Debugging.DebugWindows {
    public class EditorModelsDebugTreeNavigator : IModelsDebugNavigator {
        // === Tree fields
        TreeViewState _treeState;
        ModelsDebugTreeView _treeView;
        
        int _reloadTimer = 5;
        int _lastSelection;
        object _lastSelectionItem;

        public string SearchString {
            get => _treeView.searchString;
            set => _treeView.searchString = value;
        }

        public void Draw(Rect rect) {
            _treeView.OnGUI(rect);
        }

        public MembersList GetSelected() {
            var selections = _treeView.GetSelection();
            var selectedId = selections.Count > 0 ? selections[0] : 0;
            
            var selectedObject = _treeView.FindObject(selectedId);
            if (_lastSelection != selectedId) {
                _lastSelection = selectedId;
            }else if (_lastSelectionItem != null && selectedObject != _lastSelectionItem as MembersList) {
                var oldIndex = _treeView.FindIndex(_lastSelectionItem);
                if (oldIndex > -1) {
                    selectedId = oldIndex;
                    _treeView.SetSelection(new []{selectedId});
                    selectedObject = _treeView.FindObject(selectedId);
                    _lastSelection = selectedId;
                }
            } 
            _lastSelectionItem = selectedObject?.RelatedObject;

            return selectedObject;
        }

        public void Frame() {
            var selections = _treeView.GetSelection();
            if (selections.Count > 0 && _treeView.FindObject(selections[0]) != null) {
                _treeView.FrameItem(selections[0]);
            }
        }

        public void RefreshNavigation() {
            RefreshTree();
            ReloadTree();
        }

        public bool TrySelectModel(string modelID) {
            var newIndex = _treeView.FindIndex(modelID);
            if (newIndex != -1) {
                _treeView.SetSelection(new []{newIndex});
                _treeView.FrameItem(newIndex);
                return true;
            }
            return false;
        }

        void RefreshTree() {
            if (World.Services.Get<EventSystem>() == null) {
                _treeView = null;
                _treeState = null;
                return;
            }
                
            if (_treeState == null) {
                _treeState = new TreeViewState();
            }
            
            if (_treeView == null) {
                _treeState = new TreeViewState();
                _treeView = new ModelsDebugTreeView(_treeState);
                var listener = new ModelsDebug.DebugListenerOwner();
                World.Add(listener);
                var eventSystem = World.Services.Get<EventSystem>();
                eventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelFullyInitializedAnyType, listener, (_) => _reloadTimer = 3);
                eventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscardedAnyType, listener, (_) => _reloadTimer = 3);
            }
        }

        void ReloadTree() {
            if (_reloadTimer > -1) {
                --_reloadTimer;
            }

            if (_reloadTimer == 0) {
                _treeView?.Reload();
            }
        }
    }
}