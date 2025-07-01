using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC.Elements;
using Unity.Mathematics;

namespace Awaken.TG.Main.Fights.NPCs.Providers {
    public partial class DistancesToTargetHandler : Element<NpcElement> {
        public sealed override bool IsNotSaved => true;

        readonly List<IDesiredDistanceToTargetProvider> _providers = new();
        readonly List<IMinDistanceToTargetProvider> _minDistanceProviders = new();
        
        public static void AddDesiredDistanceToTargetProvider(NpcElement npc, IDesiredDistanceToTargetProvider provider) {
            if (!npc.TryGetElement<DistancesToTargetHandler>(out var handler)) {
                handler = npc.AddElement<DistancesToTargetHandler>();
            }
            handler.AddProvider(provider);
        }

        public static void AddMinDistanceToTargetProvider(NpcElement npc, IMinDistanceToTargetProvider provider) {
            if (!npc.TryGetElement<DistancesToTargetHandler>(out var handler)) {
                handler = npc.AddElement<DistancesToTargetHandler>();
            }
            handler.AddMinDistanceProvider(provider);
        }
        
        public static void RemoveDesiredDistanceToTargetProvider(NpcElement npc, IDesiredDistanceToTargetProvider provider) {
            npc.TryGetElement<DistancesToTargetHandler>()?.RemoveProvider(provider);
        }
        
        public static void RemoveMinDistanceToTargetProvider(NpcElement npc, IMinDistanceToTargetProvider provider) {
            npc.TryGetElement<DistancesToTargetHandler>()?.RemoveMinDistanceProvider(provider);
        }

        public static float DesiredDistanceToTarget(NpcElement npc) {
            if (npc == null) {
                return 0;
            }
            
            float? distance = npc.TryGetElement<DistancesToTargetHandler>()?.GetDesiredDistanceInternal();
            float multiplier = npc.IsTargetingHero() ? 1 : 0.5f;
            return (distance ?? npc.DefaultDesiredDistanceToTarget) * multiplier;
        }

        public static float DesiredDistanceToTarget(NpcElement npc, ICharacter target) {
            var distanceFromHandler = npc.TryGetElement<DistancesToTargetHandler>()?.GetDesiredDistanceInternal();
            var distance = distanceFromHandler ?? npc.DefaultDesiredDistanceToTarget;
            return distance * (target == Hero.Current ? 1f : 0.5f);
        }

        public static float MinDistanceToTarget(NpcElement npc) {
            if (npc == null || npc.Controller == null) {
                return 0;
            }

            NpcController controller = npc.Controller;
            if (!controller.CanOverrideMinDistanceToHero) {
                return controller.OriginalMinDistanceToTarget;
            }
            
            float? distance = npc.TryGetElement<DistancesToTargetHandler>()?.GetMinDistanceToTargetInternal();
            float multiplier = npc.IsTargetingHero() ? 1 : 0.5f;
            return (distance ?? controller.OriginalMinDistanceToTarget) * multiplier;
        }
        
        float? GetDesiredDistanceInternal() {
            int providersCount = _providers.Count;
            if (providersCount == 0) {
                return null;
            }

            float minDistance = float.MaxValue;
            for (int i = 0; i < providersCount; i++) {
                var providerDistance = _providers[i].DesiredDistanceToTarget;
                minDistance = providerDistance < minDistance ? providerDistance : minDistance;
            }

            return minDistance;
        }

        float? GetMinDistanceToTargetInternal() {
            int providersCount = _minDistanceProviders.Count;
            if (providersCount == 0) {
                return null;
            }

            float minDistance = float.MaxValue;
            for (int i = 0; i < providersCount; i++) {
                var providerDistance = _minDistanceProviders[i].MinDistanceToTarget;
                minDistance = providerDistance < minDistance ? providerDistance : minDistance;
            }

            return minDistance;
        }

        void AddProvider(IDesiredDistanceToTargetProvider provider) {
            _providers.Add(provider);
        }

        void AddMinDistanceProvider(IMinDistanceToTargetProvider provider) {
            _minDistanceProviders.Add(provider);
        }

        void RemoveProvider(IDesiredDistanceToTargetProvider provider) {
            _providers.Remove(provider);
        }

        void RemoveMinDistanceProvider(IMinDistanceToTargetProvider provider) {
            _minDistanceProviders.Remove(provider);
        }
    }
}