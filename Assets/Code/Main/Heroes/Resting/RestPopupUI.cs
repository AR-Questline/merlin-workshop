using Awaken.TG.Graphics.Transitions;
using Awaken.TG.Main.AI.SummonsAndAllies;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.FancyPanel;
using Awaken.TG.Main.Wyrdnessing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.MVC.UI.Universal;
using Awaken.TG.Utility;
using Awaken.Utility.Times;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Resting {
    [SpawnsView(typeof(VRestPopupUI))]
    public partial class RestPopupUI : Model, IUIStateSource {
        public const float FadeDuration = 0.5f;
        const int HourStep = 1;
        
        readonly bool _withTransition;
        
        public override Domain DefaultDomain => Domain.Gameplay;
        public sealed override bool IsNotSaved => true;
        public UIState UIState => UIState.ModalState(HUDState.MiddlePanelShown);

        public Transform ViewParent { get; }
        public int WeatherHour => WeatherTime.Hour;
        public int WeatherMinute => WeatherTime.Minutes;
        public int HourValueChange { get; private set; }
        public bool WillBeSurprisedByWyrdNight => !Hero.Current.HeroWyrdNight.IsHeroInWyrdness && !IsSafelyResting;

        public bool IsSafelyResting => World.Services.Get<WyrdnessService>().IsInRepeller(Hero.Current.Coords) ||
                                       !World.Services.Get<SceneService>().IsOpenWorld ||
                                       !RestingThroughTheNight();
        ARDateTime WeatherTime { get; }
        GameRealTime GameRealTime { get; }
        static Hero Hero => Hero.Current;

        public new static class Events {
            public static readonly Event<RestPopupUI, RestPopupUI> RestingStarted = new(nameof(RestingStarted));
            public static readonly Event<RestPopupUI, RestPopupUI> RestingInterrupted = new(nameof(RestingInterrupted));
            public static readonly Event<RestPopupUI, float> RestingInitiated = new(nameof(RestingInitiated));
        }

        public RestPopupUI(Transform viewParent = null, bool withTransition = false) {
            GameRealTime = World.Only<GameRealTime>();
            WeatherTime = GameRealTime.WeatherTime;
            ViewParent = viewParent;
            _withTransition = withTransition;
        }

        protected override void OnFullyInitialized() {
            HourValueChange = 1;
            TriggerChange();
        }

        public void IncreaseHourValue() {
            ChangeValueBy(HourStep);
        }

        public void DecreaseHourValue() {
            ChangeValueBy(-HourStep);
        }

        public void Rest() {
            this.Trigger(Events.RestingStarted, this);
            
            bool willBeSurprisedByWyrdNight = WillBeSurprisedByWyrdNight;
            bool isSafelyResting = IsSafelyResting;
            float timeUntilNight = ARDateTime.HoursTillNightStart(GameRealTime.WeatherTime.Date);
            float restDuration = willBeSurprisedByWyrdNight ? timeUntilNight : HourValueChange;
            
            if (RestInterruptedCheck(restDuration, out float restDurationToInterrupt)) {
                restDuration = restDurationToInterrupt;
                SkipTime(restDuration, isSafelyResting);
                ShowRestInterruptedPopup();
                this.Trigger(Events.RestingInterrupted, this);
            } else if (willBeSurprisedByWyrdNight) {
                restDuration = timeUntilNight;
                ShowSurprisedByWyrdNightPopup();
                SkipTime(restDuration, false);
            } else {
                SkipTime(restDuration, isSafelyResting);
            }

            this.Trigger(Events.RestingInitiated, restDuration);
            foreach (var heroSummon in World.All<NpcHeroSummon>().Reverse()) {
                heroSummon.Destroy();
            }
            Close();
            return;

            void SkipTime(float numberOfHoursReallyRested, bool inHeroWyrdRepeller) {
                if (_withTransition) {
                    RestWithFade(GameRealTime, numberOfHoursReallyRested, inHeroWyrdRepeller).Forget();
                } else {
                    SkipWeatherTime(Hero, GameRealTime, numberOfHoursReallyRested, inHeroWyrdRepeller);
                }
            }
            
            void ShowSurprisedByWyrdNightPopup() {
                FancyPanelType fancyPanelType = FancyPanelType.Custom;
                fancyPanelType.Spawn(this, LocTerms.WyrdNightSurprisedYou.Translate());
            }

            void ShowRestInterruptedPopup() {
                FancyPanelType fancyPanelType = FancyPanelType.Custom;
                fancyPanelType.Spawn(this, LocTerms.WyrdNightSomethingInterruptedYourSleep.Translate());
            }

            bool RestInterruptedCheck(float targetRestDuration, out float interruptedAfterTime) {
                return GameRealTime.WillSkipTimeBeInterrupted(targetRestDuration, isSafelyResting, out interruptedAfterTime);
            }
        }

        public void Close() {
            Discard();
        }
        
        public void SetHourChange(int hour) {
            var newValue = Mathf.Clamp(hour, 1, 24);

            if (newValue != HourValueChange) {
                HourValueChange = newValue;
                TriggerChange();
            }
        }

        void ChangeValueBy(int hour) {
            SetHourChange(HourValueChange + hour);
        }

        bool RestingThroughTheNight() {
            if (GameRealTime.WeatherTime.IsNight) {
                return true;
            }
            
            var currentDateTime = GameRealTime.WeatherTime.Date;
            var timeTillNightStart = ARDateTime.HoursTillNightStart(currentDateTime);
            var timeTillRestEnd = ARDateTime.HoursTill(currentDateTime.AddHours(HourValueChange), ARDateTime.NightStartTime);
            return timeTillNightStart - timeTillRestEnd <= 0;
        }

        static async UniTaskVoid RestWithFade(GameRealTime gameRealTime, float hourValue, bool isSafelyResting) {
            var mapInteractabilityBlocker = World.Add(new MapInteractabilityBlocker());
            var transition = World.Services.Get<TransitionService>();
            await transition.ToBlack(FadeDuration);
            var hero = Hero.Current;
            if (hero is not { HasBeenDiscarded: false }) {
                return;
            }

            hero.AllowNpcTeleport = true;

            SkipWeatherTime(hero, gameRealTime, hourValue, isSafelyResting);

            if (!await AsyncUtil.DelayTime(Hero.Current, 1, true)) {
                return;
            }

            hero.AllowNpcTeleport = false;
            mapInteractabilityBlocker.Discard();
            await transition.ToCamera(FadeDuration);
        }

        static void SkipWeatherTime(Hero hero, GameRealTime gameRealTime, float hourValue, bool isSafelyResting) {
            int minutes = Mathf.FloorToInt(hourValue * 60);
            hero.Trigger(Hero.Events.BeforeHeroRested, minutes);
            gameRealTime.SkipWeatherTimeBy(Mathf.FloorToInt(hourValue), minutes % 60, isSafelyResting);
            hero.Trigger(Hero.Events.AfterHeroRested, minutes);
        }
    }
}