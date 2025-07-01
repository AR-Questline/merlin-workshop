using System;
using System.Collections.Generic;
using System.Linq;

namespace Awaken.TG.Main.Scenes.SceneConstructors.SceneInitialization {
    /// <summary>
    /// Handle to track initialization progress and automate callback handling
    /// </summary>
    public class SceneInitializationHandle {
        public ICollection<ElementHandle> RemainedElements => _remainedElements; 
        [UnityEngine.Scripting.Preserve] public ICollection<ElementHandle> CompletedElements => _completedElements;
        [UnityEngine.Scripting.Preserve] public int RegisteredElementsCount => _remainedElements.Count + _completedElements.Count;
        List<ElementHandle> _remainedElements =  new List<ElementHandle>();
        List<ElementHandle> _completedElements = new List<ElementHandle>();

        [UnityEngine.Scripting.Preserve] public bool IsIdle => !_remainedElements.Any();

        /// <summary>
        /// Called when all registered elements completed
        /// </summary>
        public event Action OnInitialized;
        /// <summary>
        /// Called when remained elements collection change (on add new also on completed existing)
        /// </summary>
        public event Action OnRemainedElementsChanged;

        public ElementHandle GetNewElement(string name) {
            ElementHandle elementHandle = new ElementHandle(name);
            elementHandle.OnCompleted += ElementCompleted;
            _remainedElements.Add(elementHandle);
            OnRemainedElementsChanged?.Invoke();
            return elementHandle;
        }

        void ElementCompleted(ElementHandle elementHandle) {
            elementHandle.OnCompleted -= ElementCompleted;
            _remainedElements.Remove(elementHandle);
            _completedElements.Add(elementHandle);
            OnRemainedElementsChanged?.Invoke();
            if (_remainedElements.Count == 0) {
                OnInitialized?.Invoke();
            }
        }

        public void Clear() {
            OnInitialized = null;
            OnRemainedElementsChanged = null;
            _completedElements.Clear();
            _remainedElements.Clear();
        }

        /// <summary>
        /// Represents single and named initialization unit
        /// Can be use in using statement
        /// </summary>
        public class ElementHandle : IDisposable {
            public bool IsDone { get; private set; }
            public string Name { get; }

            internal ElementHandle(string name) {
                Name = name;
            }

            public event Action<ElementHandle> OnCompleted;

            public void Complete() {
                if (IsDone) {
                    OnCompleted = null;
                    return;
                }
                IsDone = true;
                OnCompleted?.Invoke(this);
            }

            public void Dispose() {
                Complete();
            }

            public override string ToString() {
                return Name;
            }
        }
    }
}