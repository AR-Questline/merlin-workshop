using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Cameras {
    public interface IFollowTarget : IModel {
        [UnityEngine.Scripting.Preserve] public Transform FollowTarget { get; }
    }
}