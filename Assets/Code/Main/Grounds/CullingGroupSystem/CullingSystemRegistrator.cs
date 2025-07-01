using System.Collections.Generic;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Grounds.CullingGroupSystem {
    public static class CullingSystemRegistrator {
        static readonly List<ICullingSystemRegistree> ToRegister = new();

        public static void Register(ICullingSystemRegistree registree) {
            var service = World.Services.TryGet<CullingSystem>();
            if (service != null) {
                service.Register(registree);
            } else {
                ToRegister.Add(registree);
            }
        }

        public static void Unregister(ICullingSystemRegistree registree) {
            var service = World.Services.TryGet<CullingSystem>();
            if (service != null) {
                service.Unregister(registree);
            } else {
                ToRegister.Remove(registree);
            }
        }

        public static void RegisterWaiting(CullingSystem cullingSystem) {
            foreach (var toRegister in ToRegister) {
                cullingSystem.Register(toRegister);
            }
            ToRegister.Clear();
        }
    }
}