using System.Collections.Generic;
using UnityEngine;

namespace Awaken.TG.Main.Timing.ARTime.TimeComponents {
    public interface ITimeComponent {
        void OnTimeScaleChange(float from, float to);
        void OnFixedUpdate(float fixedDeltaTime);

        Component Component { get; }

        [UnityEngine.Scripting.Preserve]
        public class Comparer : IEqualityComparer<ITimeComponent> {
            public bool Equals(ITimeComponent x, ITimeComponent y) {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                return x.Component.GetInstanceID() == y.Component.GetInstanceID();
            }

            public int GetHashCode(ITimeComponent obj) {
                return obj.Component.GetInstanceID();
            }
        }
    }
}