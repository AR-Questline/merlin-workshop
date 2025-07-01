using System.Collections.Generic;
using System.Linq;
using Awaken.TG.MVC;
using Awaken.Utility.GameObjects;
using UnityEngine;

namespace Awaken.TG.Main.Timing.ARTime.Modifiers {
    [RequireComponent(typeof(Rigidbody))]
    public class TimeVolume : MonoBehaviour {
        public float scale;
        public float delay;

        HashSet<View> _modifiedViews = new();
        HashSet<View> _viewInVolume = new();

        string _sourceID;
        string SourceID => _sourceID ??= $"{gameObject.PathInSceneHierarchy()}:TimeVolume";

        void OnTriggerStay(Collider other) {
            var view = other.GetComponentInParent<View>();
            if (view != null) {
                _viewInVolume.Add(view);
            }
        }

        void FixedUpdate() {
            foreach (var view in _viewInVolume.Where(view => !_modifiedViews.Contains(view) && view != null)) {
                view.GenericTarget.AddTimeModifier(CreateModifier());
            }
            
            foreach (var view in _modifiedViews.Where(view => !_viewInVolume.Contains(view) && view != null)) {
                view.GenericTarget.RemoveTimeModifiersFor(SourceID);
            }

            var temp = _modifiedViews;
            _modifiedViews = _viewInVolume;
            _viewInVolume = temp;
            
            _viewInVolume.Clear();
        }

        void OnDestroy() {
            foreach (var view in _modifiedViews) {
                view.GenericTarget.RemoveTimeModifiersFor(SourceID);
            }
        }

        ITimeModifier CreateModifier() {
            return new MultiplyTimeModifier(SourceID, scale, delay);
        }

        void OnValidate() {
            if (delay < 0.01f) {
                delay = 0.01f;
            }

            foreach (var c in GetComponents<Collider>()) {
                c.isTrigger = true;
            }

            GetComponent<Rigidbody>().isKinematic = true;
        }
    }
}