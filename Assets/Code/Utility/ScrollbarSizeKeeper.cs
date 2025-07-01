using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Utility {
    public class ScrollbarSizeKeeper : MonoBehaviour {
        public float size = 0;
        public bool setPositionOnStart;
        public float startPosition = 1;
        
        Scrollbar _scrollbar;
        void Awake() {
            _scrollbar = GetComponent<Scrollbar>();
            if (_scrollbar == null) {
                Destroy(this);
            }
        }

        void Start() {
            if (setPositionOnStart) {
                _scrollbar.value = startPosition;
            }
        }

        void Update() {
            _scrollbar.size = size;
        }
    }
}