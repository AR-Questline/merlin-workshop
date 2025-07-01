using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.FPP;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items.Attachments.Audio;
using Awaken.TG.Main.Heroes.MovementSystems;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Maps.Markers;
using Awaken.TG.Main.Scenes;
using Awaken.TG.Main.Tutorials;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.Maths;
using Cysharp.Threading.Tasks;
using FMODUnity;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;

namespace Awaken.TG.Main.Fights.Mounts {
    public partial class MountElement : Element<Location>, IAliveAudio, IRefreshedByAttachment<MountAttachment> {
        public override ushort TypeForSerialization => SavedModels.MountElement;

        const int FramesMargin = 5;

        [Saved] Hero _mountedHero;
        public MountData MountData {get; private set; }
        public Transform HeroTransform { get; private set; }
        public string MountName { get; private set; }

        [Saved(false)] bool _isHeroMount;

        bool _isWild;
        bool? _isVisible;

        public Hero MountedHero {
            get => _mountedHero; 
            private set => _mountedHero = value;
        }

        public bool IsHeroMount => _isHeroMount;
        public bool IsIllegal => !_isWild && !_isHeroMount;
        public bool CanUseArmor => View<VMount>()?.CanUseArmor ?? false;
        
        // === Events
        [Il2CppEagerStaticClassConstruction]
        public new static class Events {
            public static readonly Event<Hero, MountElement> HeroMounted = new(nameof(HeroMounted));
        }
        
        // === IAliveAudio
        public AliveAudio AliveAudio => ParentModel.TryGetElement<AliveAudio>();
        public void PlayAudioClip(AliveAudioType audioType, bool asOneShot = false, params FMODParameter[] eventParams) {
            PlayAudioClip(audioType.RetrieveFrom(this), asOneShot, eventParams);
        }
        public void PlayAudioClip(EventReference eventReference, bool asOneShot = false, params FMODParameter[] eventParams) {
            View<VMount>().PlayAudioClip(eventReference, asOneShot, eventParams);
        }

        public void InitFromAttachment(MountAttachment spec, bool isRestored) {
            MountData = spec.MountData;
            MountName = spec.mountName;
            _isWild = spec.wildHorse;
        }

        protected override void OnInitialize() {
            ParentModel.AddElement(new MountPetAction());
            ParentModel.AddElement(new MountAction());
            Init();
        }
        
        protected override void OnRestore() {
            Init();
        }

        void Init() {
            ParentModel.OnVisualLoaded(t => {
                VMount vMount = t.gameObject.AddComponent<VMount>();
                World.BindView(this, vMount, true, true);
                VMountEventsListener mountEventsListener = t.gameObject.AddComponent<VMountEventsListener>();
                World.BindView(this, mountEventsListener);
            });
            ParentModel.ListenTo(ICullingSystemRegistreeModel.Events.DistanceBandChanged, RefreshDistanceBand, this);
            RefreshDistanceBand(ParentModel.GetCurrentBandSafe(LocationCullingGroup.LastBand));
        }

        public void Mount(Hero hero) {
            if (hero.TrySetMovementType<MountedMovement>(out var mounted)) {
                mounted.AssignMount(this);
            } else {
                Log.Debug?.Error("Failed to set movement type to MountedMovement.");
                return;
            }
            
            if (!TutorialKeys.IsConsumed(TutKeys.TriggerHorseAcquire)) {
                TutorialMaster.Trigger(TutKeys.TriggerHorseAcquire);
            }
            
            MountedHero = hero;
            MountedHero.OwnedMount = this;
            HeroTransform = hero.MainView.transform;
            var vMount = View<VMount>();
            
            World.Only<PlayerInput>().RegisterPlayerInput(vMount);
            HeroTransform.SetParent(vMount.Saddle);
            vMount.ToggleMountState(true);
            
            ParentModel.TryGetElement<LocationMarker>()?.SetEnabled(false);

            if (!_isHeroMount) {
                CommitCrime.Theft(this, ParentModel);
                MarkAsHeroMount(true);
            }

            ResetHeroTransform().Forget();
            
            MountedHero.Trigger(Events.HeroMounted, this);
        }

        public void Dismount() {
            ParentModel.TryGetElement<LocationMarker>()?.SetEnabled(true);
            if (MountedHero is { HasBeenDiscarded: false }) {
                MoveMountedHero();
                
                MountedHero.ReturnToDefaultMovement();
                
                Hero.Current.FoV.UpdateCustomLocomotionFoVMultiplier(1f);
                
                if (!HasBeenDiscarded) {
                    var vMount = View<VMount>();
                    vMount.ToggleMountState(false);
                    World.Only<PlayerInput>().UnregisterPlayerInput(vMount);
                }
                
                MountedHero = null;
                HeroTransform = null;
            }
        }

        public void MarkAsHeroMount(bool heroMount) {
            _isHeroMount = heroMount;
        }
        
        public bool CanPetHorse() {
            Vector3 horseToHeroDirection = (Hero.Current.Coords - ParentModel.Coords).ToHorizontal3().normalized;
            float dot = Vector3.Dot(ParentModel.Forward().ToHorizontal3(), horseToHeroDirection);
            return dot > 0.8f;
        }

        void MoveMountedHero() {
            DismountPoint[] dismountLocations = View<VMount>().dismountLocations;
            Transform locationToDismount = null;
            
            foreach (var location in dismountLocations) {
                if (location.isAvailable) {
                    locationToDismount = location.transform;
                    break;
                }
            }
            
            locationToDismount ??= dismountLocations.Last().transform;
            CharacterController characterController = MountedHero.VHeroController.Controller;
            
            characterController.enabled = false;
            HeroTransform.SetPositionAndRotation(locationToDismount.position, Quaternion.LookRotation(locationToDismount.forward.X0Z()));
            MountedHero.MoveTo(locationToDismount.position);
            HeroTransform.SetParent(Services.Get<ViewHosting>().DefaultForHero());
            characterController.enabled = true;
        }
        
        void RefreshDistanceBand(int band) {
            if (LocationCullingGroup.InNpcVisibilityBand(band)) {
                if (_isVisible == true) {
                    return;
                }
                _isVisible = true;
                ParentModel.SetCulled(false);
            } else {
                if (_isVisible == false) {
                    return;
                }
                _isVisible = false;
                ParentModel.SetCulled(true);
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            HeroTransform = null;
        }

        async UniTaskVoid ResetHeroTransform() {
            if (!await AsyncUtil.DelayFrame(this, FramesMargin) || HeroTransform == null) 
                return;
            
            HeroTransform.localPosition = Vector3.zero;
            HeroTransform.localRotation = Quaternion.identity;
        }
    }
}