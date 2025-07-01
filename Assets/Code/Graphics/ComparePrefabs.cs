using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Graphics {
    [ExecuteAlways]
    public class ComparePrefabs : MonoBehaviour {
        [SerializeField] GameObject prefabA;
        [SerializeField] GameObject prefabB;
        [SerializeField, OnValueChanged(nameof(Switch)), EnableIf(nameof(HasPrefabs))]
        bool switchPrefab;

        bool HasPrefabs => prefabA && prefabB;
        
        void Switch() {
            if (switchPrefab) {
                prefabA.SetActive(true); 
                prefabB.SetActive(false); 
            } else {
                prefabA.SetActive(false); 
                prefabB.SetActive(true); 
            }
        }
    }
}
