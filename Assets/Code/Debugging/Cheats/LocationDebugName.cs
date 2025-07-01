using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility.Collections;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Debugging.Cheats {
    [UsesPrefab("Locations/" + nameof(LocationDebugName))]
    public class LocationDebugName : MonoBehaviour {
        [SerializeField] TMP_Text locationName;

        Transform _transform;

        public void Init(Location target) {
            _transform = transform;
            Transform determineHost = target.ViewParent;
            _transform.SetParent(determineHost);
            _transform.localPosition = Vector3.zero;
            
            float maxHeight = 0.3f;
            float yBase = _transform.position.y;
            determineHost.GetComponentsInChildren<Collider>().ForEach(c => {
                float yOffset = c.bounds.extents.y + c.transform.position.y - yBase;
                if (yOffset > maxHeight) {
                    maxHeight = yOffset;
                }
            });

            _transform.position += Vector3.up * maxHeight;

            locationName.text = target.Spec != null ? target.Spec.name : "NoSpec:\n" + determineHost.name;
            _transform.LookAt(Hero.Current.Coords + Vector3.up * Hero.Current.Height, Vector3.up);
            _transform.Rotate(Vector3.up, 180f);
        }

        void Update() {
            _transform.LookAt(Hero.Current.Coords + Vector3.up * Hero.Current.Height, Vector3.up);
            _transform.Rotate(Vector3.up, 180f);
        }
    }
}