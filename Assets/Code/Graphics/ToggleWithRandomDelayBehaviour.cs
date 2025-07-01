#if UNITY_EDITOR
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Graphics {
    public class ToggleWithRandomDelayBehaviour : MonoBehaviour {
        [Required] public GameObject target;
        [MinMaxSlider(minValueGetter: nameof(activeTimeRangeMin), maxValueGetter: nameof(activeTimeRangeMax), showFields: true)]
        public Vector2 activeTimeMinMax = new(0.6f, 1.2f);

        [MinMaxSlider(minValueGetter: nameof(inactiveTimeRangeMin), maxValueGetter: nameof(inactiveTimeRangeMax), showFields: true)]
        public Vector2 inactiveTimeMinMax = new(1.5f, 2.5f);

        [SerializeField, FoldoutGroup("Ranges", Expanded = false)] float activeTimeRangeMin = 0.5f;
        [SerializeField, FoldoutGroup("Ranges")] float activeTimeRangeMax = 1.5f;
        [SerializeField, FoldoutGroup("Ranges")] float inactiveTimeRangeMin = 1;
        [SerializeField, FoldoutGroup("Ranges")] float inactiveTimeRangeMax = 3;

        void Reset() {
            target = gameObject;
        }
    }
}
#endif
