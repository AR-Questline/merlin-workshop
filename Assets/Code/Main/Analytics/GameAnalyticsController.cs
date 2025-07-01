#if !UNITY_GAMECORE && !UNITY_PS5
using System;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using UnityEngine;

namespace Awaken.TG.Main.Analytics {
    /// <summary>
    /// Responsible for activating and deactivating GameAnalytics system
    /// </summary>
    public partial class GameAnalyticsController : Model {
        public override Domain DefaultDomain => Domain.Globals;
        public sealed override bool IsNotSaved => true;

        // === State
        static bool s_wasInitialized;
        static bool s_isSessionStarted;

        public new static class Events {
            public static readonly Event<GameAnalyticsController, GameAnalyticsController> SessionStarted = new(nameof(SessionStarted));
        }
        
        // === Initialization
        protected override void OnInitialize() {
            //GameAnalytics.SetEnabledManualSessionHandling(true);
            World.Only<CollectData>().ListenTo(Setting.Events.SettingRefresh, RefreshEnabled, this);
            RefreshEnabled(World.Only<CollectData>());
            InitElements();
        }

        void InitElements() {
            AddElement(new CustomDimensionsAnalytics());
            AddElement(new GeneralAnalytics());
            AddElement(new ItemsAnalytics());
            AddElement(new QuestAnalytics());
            AddElement(new HeroAnalytics());
            AddElement(new MapAnalytics());

            AddElement<AnalyticsCheatsListener>();
        }
        
        // === Callbacks
        void RefreshEnabled(Setting setting) {
            CollectData collect = (CollectData) setting;
            bool isEnabled = collect.Enabled;
            if (isEnabled && !s_isSessionStarted) {
                StartSession();
            } else if (!isEnabled && s_isSessionStarted) {
                EndSession();
            }
        }
        
        // === Helpers
        void StartSession() {
            if (!s_wasInitialized) {
                //GameAnalytics.SetBuildAllPlatforms(Application.version);
                //GameAnalytics.Initialize();
                s_wasInitialized = true;
            }

            //GameAnalytics.StartSession();
            s_isSessionStarted = true;
            this.Trigger(Events.SessionStarted, this);
        }

        void EndSession() {
            //GameAnalytics.EndSession();
            s_isSessionStarted = false;
        }

        public static void OnActiveSession(Action action, IModel owner) {
            ModelUtils.DoForFirstModelOfType<GameAnalyticsController>(controller => {
                if (s_isSessionStarted) {
                    action();
                } else {
                    controller.ListenToLimited(Events.SessionStarted, action, owner);
                }
            }, owner);
        }
    }
}
#endif