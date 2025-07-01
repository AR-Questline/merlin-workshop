using Awaken.TG.Main.General;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Orders {
    public interface IMove : IModel {
        [UnityEngine.Scripting.Preserve] Vector3 FacingDirection { get; }
        [UnityEngine.Scripting.Preserve] float Velocity { get; }
        [UnityEngine.Scripting.Preserve] float Progress { get; }
        [UnityEngine.Scripting.Preserve] bool AnyWaypointsRemaining { get; }
        [UnityEngine.Scripting.Preserve] void ResetTarget(Vector3 targetCoords, float timeElapsed);
        [UnityEngine.Scripting.Preserve] bool CanExecute { get; }
        [UnityEngine.Scripting.Preserve] void Execute();
    }
}
