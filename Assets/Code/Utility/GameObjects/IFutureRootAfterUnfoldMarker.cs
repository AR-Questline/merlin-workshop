using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.Utility.GameObjects {
    [TypeInfoBox("This object will be root after unfold." +
                 " Hierarchy unfolding can destroy this GameObject. See SceneUnfold.cs for more info.")]
    public interface IFutureRootAfterUnfoldMarker {
        GameObject GameObject { get; }
    }
}
