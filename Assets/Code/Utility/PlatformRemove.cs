using Awaken.Utility.Extensions;
using UnityEngine;

namespace Awaken.Utility {
    public class PlatformRemove : MonoBehaviour {
        [SerializeField] PlatformUtils.Platform platformWhereRemove;

        void Awake() {
            if (platformWhereRemove.HasCommonBitsFast(PlatformUtils.GetCurrentPlatform())) {
                Destroy(gameObject);
            } else {
                Destroy(this);
            }
        }
    }
}
