using UnityEngine;

namespace Awaken.TG.Main.Fights.NPCs {
    /// <summary>
    /// Marker class for getting hands of multihanded characters when MainHand and OffHand are already used.
    /// </summary>
    public class AdditionalHandMarker : MonoBehaviour {
        [SerializeField] AdditionalHand hand;
        public AdditionalHand Hand => hand;
    }

    public enum AdditionalHand : byte {
        [UnityEngine.Scripting.Preserve] Hand1 = 0,
        [UnityEngine.Scripting.Preserve] Hand2 = 1,
        [UnityEngine.Scripting.Preserve] Hand3 = 2,
        [UnityEngine.Scripting.Preserve] Hand4 = 3,
        [UnityEngine.Scripting.Preserve] AdditionalMainHand = 4,
        [UnityEngine.Scripting.Preserve] AdditionalOffHand = 5
    }
}