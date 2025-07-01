using System.Collections.Generic;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Locations.Regrowables {
    public static class RegrowableInitialization {
        static readonly List<Regrowable> ToInitialize = new();

        public static void Initialize(Regrowable regrowable) {
            if (World.Services.TryGet(out RegrowableService regrowableService)) {
                regrowable.Initialize(regrowableService);
            } else {
                ToInitialize.Add(regrowable);
            }
        }

        public static void Uninitialize(Regrowable regrowable) {
            regrowable.Uninitialize();
            ToInitialize.Remove(regrowable);
        }

        public static void InitializeWaiting(RegrowableService regrowableService) {
            foreach (var regrowable in ToInitialize) {
                regrowable.Initialize(regrowableService);
            }
            ToInitialize.Clear();
        }
    }
}