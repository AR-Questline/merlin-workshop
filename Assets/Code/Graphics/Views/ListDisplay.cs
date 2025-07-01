using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Awaken.TG.Graphics.Views
{
    public class ListDisplay : MonoBehaviour {
        // === Editable properties

        public GameObject prefab;

        public Vector3 offset = Vector3.zero;
        [Range(0f, 1f)]
        public float alignment = 0f;

        // === Internal types

        struct Element {
            public object obj;
            public IShows display;
        }

        // === Fields

        List<Element> _currentElements = new List<Element>();

        // === Accessing the spawned objects
        [UnityEngine.Scripting.Preserve]
        public IEnumerable<T> GetDisplayed<T>() {
            return _currentElements.Select(elem => (T)elem.display);
        }

        // === Updating

        public void UpdateContents(IEnumerable<object> shownObjects) {
            var desired = shownObjects.ToArray();
            var current = _currentElements;
            // remove all stale elements
            for (int i = 0; i < current.Count;) {
                if (i >= desired.Length || !desired[i].Equals(current[i].obj)) {
                    current[i].display.Hide();
                    current.RemoveAt(i);
                } else {
                    i++;
                }
            }
            // add missing elements
            for (int i = 0; i < desired.Length; i++) {                
                if (i >= current.Count || !desired[i].Equals(current[i].obj)) {
                    GameObject instance = UnityEngine.Object.Instantiate(prefab, transform, worldPositionStays: false);
                    IShows display = instance.GetComponent<IShows>();
                    display.Show(desired[i]);
                    var element = new Element {obj = desired[i], display = display};
                    if (i < current.Count) {
                        current.Insert(i, element);
                    } else {
                        current.Add(element);
                    }
                }                
            }
            // update positions       
            int count = current.Count;
            float multiplier = -alignment * (count - 1);
            foreach (var e in current) {
                e.display.MoveTo(offset * multiplier, Quaternion.identity);
                multiplier += 1f;
            }
        }
    }

    public interface IShows {
        void Show(object elementToShow);
        void MoveTo(Vector3 position, Quaternion rotation);
        void Hide();
    }
}
