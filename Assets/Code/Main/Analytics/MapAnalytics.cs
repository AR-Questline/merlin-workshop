#if !UNITY_GAMECORE && !UNITY_PS5
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Discovery;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using UnityEngine;

namespace Awaken.TG.Main.Analytics {
    public partial class MapAnalytics : Element<GameAnalyticsController> {
        const int HeatMapGridSize = 128;

        public sealed override bool IsNotSaved => true;

        string LocationName(Location location) => NiceName(location.Spec.name);
        string NiceName(string id, int limit = 32) => AnalyticsUtils.EventName(id, limit);
        string CurrentScene => NiceName(World.Services.Get<SceneService>()?.ActiveSceneRef?.Name);

        int HeroLevel => AnalyticsUtils.HeroLevel;
        float PlayTime => AnalyticsUtils.PlayTime;

        // === Initialization
        protected override void OnInitialize() {
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelFullyInitialized<Hero>(), this, OnHeroInit);
            World.EventSystem.ListenTo(EventSelector.AnySource, LocationDiscovery.Events.LocationDiscovered, this, OnLocationDiscovered);
            World.EventSystem.ListenTo(EventSelector.AnySource, Location.Events.LocationCleared, this, OnLocationCleared);
        }

        // === Callbacks
        void OnHeroInit(Model model) {
            Hero hero = (Hero) model;

            hero.ListenTo(Hero.Events.Died, OnHeroDeath, this);
            hero.ListenTo(Hero.Events.ArrivedAtPortal, OnMapEntered, this);
            hero.ListenTo(Hero.Events.WalkedThroughPortal, OnMapExit, this);
        }

        void OnHeroDeath(DamageOutcome outcome) {
            if (outcome.Target is not Hero hero) {
                return;
            }
            Vector3 heroPos = hero.CharacterView.transform.position;
            int heroPosX = Mathf.RoundToInt(heroPos.x / HeatMapGridSize) * HeatMapGridSize;
            int heroPosZ = Mathf.RoundToInt(heroPos.z / HeatMapGridSize) * HeatMapGridSize;
            string heroPosition = $"x{heroPosX}z{heroPosZ}";
            string evt = $"DeathHeatmap:{CurrentScene}:{heroPosition}";
            AnalyticsUtils.TrySendDesignEvent($"Map:{evt}:HeroLevel", HeroLevel);
            AnalyticsUtils.TrySendDesignEvent($"Map:{evt}:PlayTime", PlayTime);
        }
        
        void OnMapEntered() {
            string evt = $"SceneEntered:{CurrentScene}";
            AnalyticsUtils.TrySendDesignEvent($"Map:{evt}:HeroLevel", HeroLevel);
            AnalyticsUtils.TrySendDesignEvent($"Map:{evt}:PlayTime", PlayTime);
        }

        void OnMapExit() {
            var enterTime = World.Services.Get<SceneService>().ActiveSceneLoadTime;
            var exitTime = World.Only<GameRealTime>().PlayRealTime;
            float timeOnScene = (exitTime - enterTime).TotalMinutes;
            string evt = $"SceneLeft:{CurrentScene}";
            AnalyticsUtils.TrySendDesignEvent($"Map:{evt}:TimeOnScene", timeOnScene);
        }

        void OnLocationDiscovered(Location location) {
            string evt = $"LocationDiscovered:{LocationName(location)}";
            AnalyticsUtils.TrySendDesignEvent($"Map:{evt}:PlayTime", PlayTime);
        }
        
        void OnLocationCleared(Location location) {
            string evt = $"LocationCleared:{LocationName(location)}";
            AnalyticsUtils.TrySendDesignEvent($"Map:{evt}:PlayTime", PlayTime);
        }
    }
}
#endif