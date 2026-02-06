using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes.Items.Attachments.Audio;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Locations.Views;
using Awaken.TG.Main.NewGamePlus;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.Main.Utility.VFX;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Cysharp.Threading.Tasks;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class AliveLocation : Element<Location>, IAlive, ILocationElement, IRefreshedByAttachment<AliveLocationAttachment> {
        public override ushort TypeForSerialization => SavedModels.AliveLocation;

        // === Fields & Properties
        AliveLocationAttachment _spec;
        [Saved] bool _discarding;

        public NpcChunk NpcChunk { get; set; }
        public SurfaceType AudioSurfaceType { get; private set; }
        public ShareableARAssetReference HitVFX { get; private set; }
        public HealthElement HealthElement => Element<HealthElement>();
        
        // === IAlive
        public bool IsAlive => HealthElement.Health.ModifiedInt > 0;
        public bool IsDying => HealthElement.Health.ModifiedInt <= 0;
        public bool Grounded => true;
        public AliveStats AliveStats => Element<AliveStats>();
        public AliveStats.ITemplate AliveStatsTemplate => Spec;
        public LimitedStat Health => AliveStats.Health;
        public Stat MaxHealth => AliveStats.MaxHealth;
        public int NewGamePlusLevel => NewGamePlusSystem.Level;
        public Transform ParentTransform { get; private set; }
        public AliveAudio AliveAudio => ParentModel.TryGetElement<AliveAudio>();
        public AliveLocationAttachment Spec => _spec;

        public Vector3 Coords => ParentModel.Coords;
        public Quaternion Rotation => ParentModel.Rotation;

        public void PlayAudioClip(AliveAudioType audioType, bool asOneShot = false, params FMODParameter[] eventParams) {
            PlayAudioClip(audioType.RetrieveFrom(this), asOneShot, eventParams);
        }
        public void PlayAudioClip(EventReference eventReference, bool asOneShot = false, params FMODParameter[] eventParams) {
            ParentModel?.LocationView?.PlayAudioClip(eventReference, asOneShot, null, eventParams);
        }
        // === VFX
        public AliveVfx AliveVfx => ParentModel.TryGetElement<AliveVfx>();

        // === Constructors
        public void InitFromAttachment(AliveLocationAttachment spec, bool isRestored) {
            _spec = spec;
            AudioSurfaceType = spec.surfaceType.EnumAs<SurfaceType>();
            HitVFX = spec.hitVFXReference;
        }

        // === Initialization
        protected override void OnInitialize() {
            AliveStats.Create(this);
            CommonInit();
        }

        protected override void OnRestore() {
            if (_discarding) {
                ParentModel.Discard();
                return;
            }
            CommonInit();
        }

        void CommonInit() {
            AddElement(new HealthElement());
            ParentModel.OnVisualLoaded(AfterVisualLoaded);
            ParentModel.ListenTo(GroundedEvents.TeleportRequested, _ => this.Trigger(GroundedEvents.TeleportRequested, this), this);
            ParentModel.ListenTo(GroundedEvents.AfterTeleported, _ => this.Trigger(GroundedEvents.AfterTeleported, this), this);
            ParentModel.ListenTo(GroundedEvents.BeforeTeleported, _ => this.Trigger(GroundedEvents.BeforeTeleported, this), this);
            ParentModel.ListenTo(GroundedEvents.AfterMoved, _ => this.Trigger(GroundedEvents.AfterMoved, this), this);
            ParentModel.ListenTo(GroundedEvents.AfterMovedToPosition, coords => this.Trigger(GroundedEvents.AfterMovedToPosition, coords), this);
        }

        void AfterVisualLoaded(Transform parentTransform) {
            ParentTransform = parentTransform;

            this.ListenTo(IAlive.Events.BeforeDeath, _ => {
                if (_spec.deathVFXReference.IsSet) {
                    VFXManager.SpawnCombatVFX(_spec.deathVFXReference, ParentTransform.position, ParentTransform.forward, null).Forget();
                }

                if (_spec.StoryOnDeath is { IsValid: true }) {
                    Story.StartStory(StoryConfig.Base(_spec.StoryOnDeath, null).WithLocation(ParentModel));
                }

                if (_spec.discardOnDeath) {
                    DiscardOnDeath().Forget();
                }
            }, this);
        }

        // === IWithStats
        public Stat Stat(StatType statType) {
            if (statType is AliveStatType aliveStats) {
                return aliveStats.RetrieveFrom(this);
            }
            return null;
        }
        
        // === Helpers
        async UniTaskVoid DiscardOnDeath() {
            _discarding = true;
            if (_spec.makeInactiveOnDeath) {
                ParentModel.SetInteractability(LocationInteractability.Inactive);
            }
            if (_spec.discardDelayInSeconds > 0 && !await AsyncUtil.DelayTime(this, _spec.discardDelayInSeconds)) {
                return;
            }
            ParentModel.Discard();
        }
    }
}
