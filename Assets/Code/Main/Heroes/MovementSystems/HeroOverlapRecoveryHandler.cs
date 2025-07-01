using System.Collections.Generic;
using System.Linq;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.MovementSystems {
    [Il2CppEagerStaticClassConstruction]
    public partial class HeroOverlapRecoveryHandler : Element<Hero> {
        public sealed override bool IsNotSaved => true;

        public static HeroOverlapRecoveryHandler Instance => World.Any<HeroOverlapRecoveryHandler>();

        readonly HashSet<IOverlapRecoveryProvider> _overlapProviders = new();
        readonly HashSet<IOverlapRecoveryDisablingBlocker> _overlapDisablingBlockers = new();
        float _overlapDisablingTimeCooldown;
        int _overlapDisablingFrameCooldown;

        bool OverlapRecoverAvailable => OverlapDisablingBlocked || _overlapProviders.Count <= 0 || _overlapProviders.All(p => !p.DisableOverlapRecovery);
        bool OverlapDisablingBlocked => _overlapDisablingBlockers.Count > 0 || _overlapDisablingTimeCooldown > Time.time || _overlapDisablingFrameCooldown > Time.frameCount;

        public static void AddOverlapRecoveryProvider(IOverlapRecoveryProvider overlapRecoveryProvider) {
            Instance?._overlapProviders.Add(overlapRecoveryProvider);
        }

        public static void RemoveOverlapRecoveryProvider(IOverlapRecoveryProvider overlapRecoveryProvider) {
            Instance?._overlapProviders.Remove(overlapRecoveryProvider);
        }
        
        public static void AddOverlapRecoveryDisablingBlocker(IOverlapRecoveryDisablingBlocker overlapRecoveryDisablingBlocker) {
            Instance?._overlapDisablingBlockers.Add(overlapRecoveryDisablingBlocker);
        }

        public static void RemoveOverlapRecoveryDisablingBlocker(IOverlapRecoveryDisablingBlocker overlapRecoveryDisablingBlocker) {
            Instance?._overlapDisablingBlockers.Remove(overlapRecoveryDisablingBlocker);
            Instance?.PutOverlapOnCooldown();
        }

        public static bool CanRecoverFromOverlap() {
            return Instance?.OverlapRecoverAvailable ?? true;
        }

        protected override void OnInitialize() {
            ParentModel.ListenTo(Hero.Events.HeroLongTeleported, PutOverlapOnCooldown, this);
            ParentModel.ListenTo(Hero.Events.WalkedThroughPortal, PutOverlapOnCooldown, this);
            ParentModel.ListenTo(Hero.Events.ArrivedAtPortal, PutOverlapOnCooldown, this);
        }
        
        void PutOverlapOnCooldown() {
            const float TimeCooldown = 0.5f;
            const int FrameCooldown = 3;
            
            _overlapDisablingTimeCooldown = Time.time + TimeCooldown;
            _overlapDisablingFrameCooldown = Time.frameCount + FrameCooldown;
        }
    }
}