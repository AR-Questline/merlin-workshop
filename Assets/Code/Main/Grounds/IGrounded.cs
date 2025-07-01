using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;

namespace Awaken.TG.Main.Grounds {
    /// <summary>
    /// Implemented by models that are placed on the map.
    /// </summary>
    public interface IGrounded : IModel {
        Vector3 Coords { get; }
        Quaternion Rotation { get; }
    }

    [Il2CppEagerStaticClassConstruction]
    public static class GroundedEvents {
        /// <summary>
        /// Event called each time IGrounded moves
        /// </summary>
        public static readonly Event<IGrounded, IGrounded> AfterMoved = new(nameof(AfterMoved));
        /// <summary>
        /// Event called each time IGrounded moves
        /// </summary>
        public static readonly Event<IGrounded, Vector3> AfterMovedToPosition = new(nameof(AfterMovedToPosition));
        /// <summary>
        /// Event called before any teleportation logic after that IGrounded can be in odd position
        /// </summary>
        public static readonly Event<IGrounded, IGrounded> TeleportRequested = new(nameof(TeleportRequested));
        /// <summary>
        /// Event called before IGrounded moves position via teleport
        /// </summary>
        public static readonly Event<IGrounded, IGrounded> BeforeTeleported = new(nameof(BeforeTeleported));
        /// <summary>
        /// Event called after IGrounded moves position via teleport
        /// </summary>
        public static readonly Event<IGrounded, IGrounded> AfterTeleported = new(nameof(AfterTeleported));
    }

    public static class GroundedExtension {
        public static Vector3 Forward(this IGrounded grounded) {
            return grounded.Rotation*Vector3.forward;
        }
        public static Vector3 Right(this IGrounded grounded) {
            return grounded.Rotation*Vector3.right;
        }
        
        [UnityEngine.Scripting.Preserve]
        public static Vector3 Up(this IGrounded grounded) {
            return grounded.Rotation*Vector3.up;
        }
    }
}