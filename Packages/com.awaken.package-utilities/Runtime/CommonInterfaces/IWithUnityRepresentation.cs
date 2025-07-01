using UnityEngine;

namespace Awaken.CommonInterfaces {
    public interface IWithUnityRepresentation {
        void SetUnityRepresentation(in Options options);

        public struct Options {
            public bool? requiresEntitiesAccess;
            public bool? linkedLifetime;
            public bool? movable;
        }
    }

    public static class WithUnityLifetimeExtensions {
        public static void SetUnityRepresentation(this GameObject instance, in IWithUnityRepresentation.Options options) {
            var withUnityLifetimes = instance.GetComponentsInChildren<IWithUnityRepresentation>(true);
            foreach (var withUnityLifetime in withUnityLifetimes) {
                withUnityLifetime.SetUnityRepresentation(options);
            }
        }
    }
}
