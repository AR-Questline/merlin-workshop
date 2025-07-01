using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.UI.RawImageRendering {
    public class HeroRendererSockets : MonoBehaviour {
        [field: SerializeField, BoxGroup("Items Sockets")] public Transform HeadSocket { [UnityEngine.Scripting.Preserve] get; private set; }
        [field: SerializeField, BoxGroup("Items Sockets")] public Transform MainHandSocket { [UnityEngine.Scripting.Preserve] get; private set; }
        [field: SerializeField, BoxGroup("Items Sockets")] public Transform OffHandSocket { [UnityEngine.Scripting.Preserve] get; private set; }
        [field: SerializeField, BoxGroup("Items Sockets")] public Transform RootSocket { [UnityEngine.Scripting.Preserve] get; private set; }
    }
}