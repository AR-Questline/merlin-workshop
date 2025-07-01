using Awaken.TG.Main.Heroes.WyrdStalker;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.Wyrdnessing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;

namespace Awaken.TG.Main.Heroes {
    public partial class HeroWyrdNight : Element<Hero> {
        public override ushort TypeForSerialization => SavedModels.HeroWyrdNight;

        static StatusTemplate WyrdNightStatusTemplate => CommonReferences.Get.WyrdNightStatus;

        // === Fields
        [Saved] WeakModelRef<Status> _wyrdNightStatus;
        bool _isUnderWyrdNightRepellerInfluence;
        bool _isNight;

        // === Properties
        public bool Night => _isNight;
        public bool IsHeroInWyrdness => _wyrdNightStatus.TryGet(out _);
        public HeroWyrdStalker WyrdStalker { get; private set; }

        public new static class Events {
            public static readonly Event<Hero, bool> WyrdNightChanged = new(nameof(WyrdNightChanged));
            public static readonly Event<Hero, bool> StatusChanged = new(nameof(StatusChanged));
            /// <summary>
            /// Will not trigger during daytime
            /// </summary>
            public static readonly Event<Hero, bool> RepellerChanged = new(nameof(RepellerChanged));
        }

        // === Initialization
        protected override void OnInitialize() {
            WyrdStalker = AddElement<HeroWyrdStalker>();
            Init();
        }

        protected override void OnRestore() {
            WyrdStalker = Element<HeroWyrdStalker>();
            Init();
        }

        void Init() {
            var gameTime = World.Only<GameRealTime>();
            gameTime.NightChanged += OnNightChanged;
            _isNight = gameTime.WeatherTime.IsNight;
        }
        
        // === Updating
        void OnNightChanged(bool isNight) {
            _isNight = isNight;
            ParentModel.Trigger(Events.WyrdNightChanged, isNight);
            
            if (_isNight) {
                ParentModel.Trigger(Events.RepellerChanged, _isUnderWyrdNightRepellerInfluence);
            }
        }
        
        public void OnWyrdNightRepellerChanged() {
            UpdateWyrdNightStatus();
            _isUnderWyrdNightRepellerInfluence = ParentModel.IsSafeFromWyrdness;
            
            if (_isNight) {
                ParentModel.Trigger(Events.RepellerChanged, _isUnderWyrdNightRepellerInfluence);
            }
        }
        
        void UpdateWyrdNightStatus() {
            bool shouldBeActive = !ParentModel.IsSafeFromWyrdness;

            if (shouldBeActive && _wyrdNightStatus.Get() == null) {
                _wyrdNightStatus = ParentModel.Statuses.AddStatus(WyrdNightStatusTemplate,
                    StatusSourceInfo.FromStatus(WyrdNightStatusTemplate).WithCharacter(ParentModel)).newStatus;
                ParentModel.Trigger(Events.StatusChanged, true);
            } else if (!shouldBeActive && _wyrdNightStatus.TryGet(out var status)) {
                ParentModel.Statuses.RemoveStatus(status);
                _wyrdNightStatus = null;
                ParentModel.Trigger(Events.StatusChanged, false);
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            var gameTime = World.Any<GameRealTime>();
            if (gameTime) {
                gameTime.NightChanged -= OnNightChanged;
            }
            base.OnDiscard(fromDomainDrop);
        }
    }
}