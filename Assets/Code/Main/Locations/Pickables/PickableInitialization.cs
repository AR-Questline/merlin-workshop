using System.Collections.Generic;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Locations.Pickables {
    public static class PickableInitialization {
        static readonly List<Pickable> ToInitialize = new();

        public static void Initialize(Pickable pickable) {
            if (World.Services.TryGet(out PickableService pickableService)) {
                pickable.Initialize(pickableService);
            } else {
                ToInitialize.Add(pickable);
            }
        }

        public static void Uninitialize(Pickable regrowable) {
            regrowable.Uninitialize();
            ToInitialize.Remove(regrowable);
        }

        public static void InitializeWaiting(PickableService pickableService) {
            foreach (var regrowable in ToInitialize) {
                regrowable.Initialize(pickableService);
            }
            ToInitialize.Clear();
        }
    }
}