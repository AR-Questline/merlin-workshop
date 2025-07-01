using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.WyrdStalker {
    public partial class HeroWyrdStalker : Element<HeroWyrdNight> {
        public override ushort TypeForSerialization => SavedModels.HeroWyrdStalker;

        public const int ActiveWyrdStalkerThreshold = 3;
        
        WyrdStalkerControllerBase _controllerBase;
        
        [Saved] public bool WyrdStalkerDead { get; private set; }
        public int SoulsPickedUpCount => Hero.Development.WyrdSoulFragmentsCount;
        public Hero Hero => ParentModel.ParentModel;

        public float AudioProximity {
            get {
                if (_controllerBase is not { Spawned: true } || !_controllerBase.WyrdStalker.TryGet(out var wyrdstalker)) {
                    return 0f;
                }
                var distance = (wyrdstalker.Coords - Hero.Coords).magnitude;
                var range = GameConstants.Get.wyrdStalkerAudioRange;
                var proximity = 1f - Mathf.Clamp01((distance - range.min) / (range.max - range.min));
                if (_controllerBase.WasVisible && proximity > GameConstants.Get.wyrdStalkerNeverSeenMaxAudioThreshold) {
                    proximity = GameConstants.Get.wyrdStalkerNeverSeenMaxAudioThreshold;
                }
                return proximity;
            }
        }

        public bool IsHeroInWyrdness => ParentModel.IsHeroInWyrdness;
        
        protected override void OnInitialize() {
            Hero.ListenTo(HeroWyrdNight.Events.WyrdNightChanged, OnWyrdNightChanged, this);
            Hero.ListenTo(HeroFoV.Events.FoVUpdated, OnFoVChanged, this);
            Hero.ListenTo(Hero.Events.WyrdSoulFragmentCollected, OnSoulPickedUp, this);
            CreateController();
            OnWyrdNightChanged(ParentModel.Night);
        }

        public bool TrySpawn(bool ignoreRequirements = false) {
            return _controllerBase.TrySpawn(ignoreRequirements);
        }

        void NightUpdate(float deltaTime) {
            _controllerBase.NightUpdate(deltaTime);
        }

        // Controller
        public void RefreshController() {
            RemoveController();
            CreateController();
        }
        
        void CreateController() {
            if (SoulsPickedUpCount < ActiveWyrdStalkerThreshold) {
                _controllerBase = new PassiveWyrdStalkerControllerBase(this);
            } else {
                _controllerBase = new ActiveWyrdStalkerControllerBase(this);
            }
        }

        void RemoveController() {
            _controllerBase.HideWyrdStalker();
            _controllerBase = null;
        }

        // Callbacks
        void OnWyrdNightChanged(bool isNight) {
            if (isNight) {
                this.GetOrCreateTimeDependent().WithUpdate(NightUpdate);
            } else {
                _controllerBase.HideWyrdStalker();
                this.GetTimeDependent()?.WithoutUpdate(NightUpdate);
            }
        }
        
        void OnFoVChanged(HeroFoV.FoVChangeData data) {
            _controllerBase.UpdateFoV(data.newFoV);
        }

        void OnSoulPickedUp() {
            if (SoulsPickedUpCount == ActiveWyrdStalkerThreshold) {
                RefreshController();
            }
            WyrdStalkerDead = false;
        }
        
        public void OnWyrdStalkerDeath() {
            WyrdStalkerDead = true;
        }
        
        // Lifecycle
        
        protected override void OnDiscard(bool fromDomainDrop) {
            RemoveController();
            this.GetTimeDependent()?.WithoutUpdate(NightUpdate);
        }
    }
}