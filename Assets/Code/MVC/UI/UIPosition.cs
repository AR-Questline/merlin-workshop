using UnityEngine;

namespace Awaken.TG.MVC.UI
{
    /// <summary>
    /// Represents a position for UI purposes. The position is tracked in
    /// different spaces - clients can use whichever type of position
    /// is most useful to them.
    /// </summary>
    public struct UIPosition {
        /// <summary>
        /// World position that we are interacting with. This position always
        /// assumes y=0.
        /// </summary>
        [UnityEngine.Scripting.Preserve] public Vector3 world;
        /// <summary>
        /// The screen position of the interaction.
        /// </summary>
        public Vector2 screen;
    }

    public struct NullableVector3 {
        public Vector3? vector;
        public static implicit operator Vector3(NullableVector3 nullableVector) {
            return nullableVector.vector ?? Vector3.zero;
        }

        public static implicit operator NullableVector3(Vector3 vector3) {
            return new NullableVector3{vector = vector3};
        }
    }
}
