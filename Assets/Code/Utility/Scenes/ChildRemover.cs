using Sirenix.OdinInspector;
using UnityEngine;
using Random = System.Random;

namespace Awaken.Utility.Scenes {
    public class ChildRemover : ScenePreProcessorComponent {
        [InfoBox("Removes given percentage of children randomly.")]
        [SerializeField] int seed;
        [SerializeField, Range(0, 1)] float percent;

        public override void Process() {
            var childCount = transform.childCount;
            var toDelete = Mathf.FloorToInt(childCount * percent);
            var random = new Random(seed);
            for (var i = 0; i < toDelete; i++) {
                var index = random.Next(childCount);
                var child = transform.GetChild(index);
                DestroyImmediate(child.gameObject);
                --childCount;
            }
        }
        
        void Reset() {
            seed = new Random().Next();
        }
    }
}