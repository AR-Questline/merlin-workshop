using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI.Universal;
using Awaken.Utility.Debugging;
using UnityEngine;
using Debug = UnityEngine.Debug;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.MVC.UI.Handlers.Focuses {
    public partial class FocusHistorian : Element<Focus> {
        const int Size = 200;

        public sealed override bool IsNotSaved => true;

        List<Component> _waiting = new List<Component>();
        List<Component> _history = new List<Component>();
        HashSet<Component> _index = new HashSet<Component>();

        protected override void OnInitialize() {
            this.GetOrCreateTimeDependent().WithLateUpdate(ProcessLateUpdate).ThatProcessWhenPause();
        }

        public void RegisterFocusChange(Component current, bool isFromInit) {
            if (current == null) return;

            if (isFromInit) {
                // waiting queue is needed only because it allows to focus objects in order of their creation
                if (!_waiting.Contains(current)) {
                    _waiting.Add(current);
                }
                return;
            }
            
            if (_index.Add(current)) {
                _history.Add(current);
                if (_history.Count > Size) {
                    RefreshQueue();
                }
            } else {
                ChangeToLast(current);
            }
        }

        public void Erase(Component component) {
            _history.Remove(component);
            _index.Remove(component);
            _waiting.Remove(component);
        }

        public void ClearInvalidEntries() {
            RefreshQueue();
        }

        void RefreshQueue() {
            _waiting.RemoveAll(static s => s == null);
            _history.RemoveAll(static s => s == null);
            _index.RemoveWhere(static s => s == null);

            while (_history.Count > Size) {
                _index.Remove(_history[0]);
                _history.RemoveAt(0);
            }

            while (_waiting.Count > Size) {
                _waiting.RemoveAt(0);
            }
        }

        void ProcessLateUpdate(float deltaTime) {
            if (!RewiredHelper.IsGamepad) {
                return;
            }
            
            if (ParentModel.Focused == null || !ParentModel.Focused.gameObject.activeInHierarchy) {
                RefreshQueue();
                Component last = _history.LastOrDefault(c => ParentModel.BelongsToFocusBase(c) && NavigationNotBlocked(c));
                if (last == null) {
                    last = _waiting.FirstOrDefault(c => ParentModel.BelongsToFocusBase(c) && NavigationNotBlocked(c));
                }
                if (last != null) {
                    if (Focus.DebugMode) {
                        Log.Important?.Error($"Historian changes to {last.gameObject.name}", last.gameObject);
                    }
                    ParentModel.Select(last);
                    _waiting.Remove(last);
                }
            }
        }

        bool NavigationNotBlocked(Component c) {
            return c.GetComponent<INaviBlocker>()?.AllowNavigation ?? true;
        }

        void ChangeToLast(Component obj) {
            int currentIndex = _history.IndexOf(obj);
            _history.RemoveAt(currentIndex);
            _history.Add(obj);
        }
    }
}